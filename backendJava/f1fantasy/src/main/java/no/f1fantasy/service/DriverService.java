package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Driver;
import no.f1fantasy.repository.DriverRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.time.Year;
import java.util.ArrayList;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Objects;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

@Service
public class DriverService {

    private static final Logger logger = LoggerFactory.getLogger(DriverService.class);
    private static final String PAGINATION_KEY = "drivers";

    private final ErgastApiClient ergastApiClient;
    private final DriverRepository driverRepository;
    private final PaginationStateTracker paginationStateTracker;
    private final ObjectMapper objectMapper;

    public DriverService(
        ErgastApiClient ergastApiClient,
        DriverRepository driverRepository,
        PaginationStateTracker paginationStateTracker,
        ObjectMapper objectMapper
    ) {
        this.ergastApiClient = ergastApiClient;
        this.driverRepository = driverRepository;
        this.paginationStateTracker = paginationStateTracker;
        this.objectMapper = objectMapper;
    }

    public List<Driver> getAllDrivers() {
        List<Driver> cached = driverRepository.findAll();
        if (!paginationStateTracker.shouldFetch(PAGINATION_KEY)) {
            return cached;
        }

        int limit = 30;
        int offset = paginationStateTracker.getNextOffset(PAGINATION_KEY, limit);
        int total = 0;

        try {
            do {
                String payload = ergastApiClient.getJson("/drivers/?offset=" + offset);
                JsonNode mrData = objectMapper.readTree(payload).path("MRData");
                total = mrData.path("total").asInt(0);
                JsonNode drivers = mrData.path("DriverTable").path("Drivers");

                if (!drivers.isArray() || drivers.isEmpty()) {
                    break;
                }

                List<Driver> batch = new ArrayList<>();
                for (JsonNode driverNode : drivers) {
                    batch.add(readDriver(driverNode));
                }

                driverRepository.saveAll(batch);
                paginationStateTracker.updateState(PAGINATION_KEY, offset, total, limit);
                offset += limit;
            } while (offset < total);

            if (total == 0 || offset >= total) {
                paginationStateTracker.markComplete(PAGINATION_KEY);
            }

            return driverRepository.findAll();
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch drivers from Ergast, returning cached data", ex);
            return cached;
        }
    }

    public List<Driver> getDriversBySeason(String season) {
        List<Driver> cached = driverRepository.findAll();

        try {
            String payload = ergastApiClient.getJson("/" + season + "/drivers/");
            JsonNode drivers = objectMapper.readTree(payload)
                .path("MRData")
                .path("DriverTable")
                .path("Drivers");

            if (!drivers.isArray()) {
                return cached;
            }

            List<Driver> parsed = new ArrayList<>();
            for (JsonNode driverNode : drivers) {
                Driver driver = readDriver(driverNode);
                addActiveSeason(driver, season);
                parsed.add(driver);
            }

            mergeExistingActiveSeasons(parsed, season);
            driverRepository.saveAll(parsed);
            return parsed;
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch drivers for season {}, returning cached data", season, ex);
            return cached;
        }
    }

    public Optional<Driver> getDriverById(String driverId) {
        Optional<Driver> driver = driverRepository.findByDriverId(driverId);
        if (driver.isPresent()) {
            return driver;
        }

        getAllDrivers();
        return driverRepository.findByDriverId(driverId);
    }

    public List<Driver> getCachedDrivers() {
        return driverRepository.findAll();
    }

    public List<Driver> getActiveDrivers(String season) {
        String targetSeason = season == null || season.isBlank() ? String.valueOf(Year.now().getValue()) : season;

        List<Driver> active = driverRepository.findAll().stream()
            .filter(driver -> driver.getActiveSeasons() != null && driver.getActiveSeasons().contains(targetSeason))
            .toList();

        if (!active.isEmpty()) {
            return active;
        }

        getDriversBySeason(targetSeason);
        return driverRepository.findAll().stream()
            .filter(driver -> driver.getActiveSeasons() != null && driver.getActiveSeasons().contains(targetSeason))
            .toList();
    }

    private Driver readDriver(JsonNode node) {
        Driver driver = new Driver();
        driver.setDriverId(node.path("driverId").asText());
        driver.setPermanentNumber(node.path("permanentNumber").asText(null));
        driver.setCode(node.path("code").asText(null));
        driver.setUrl(node.path("url").asText(null));
        driver.setGivenName(node.path("givenName").asText(null));
        driver.setFamilyName(node.path("familyName").asText(null));
        driver.setDateOfBirth(node.path("dateOfBirth").asText(null));
        driver.setNationality(node.path("nationality").asText(null));
        return driver;
    }

    private void mergeExistingActiveSeasons(List<Driver> parsed, String fetchedSeason) {
        List<String> driverIds = parsed.stream().map(Driver::getDriverId).collect(Collectors.toList());
        List<Driver> existing = driverRepository.findAllById(Objects.requireNonNull(driverIds, "driverIds"));

        for (Driver parsedDriver : parsed) {
            Set<String> merged = new LinkedHashSet<>();
            Driver existingDriver = existing.stream()
                .filter(item -> item.getDriverId().equals(parsedDriver.getDriverId()))
                .findFirst()
                .orElse(null);

            if (existingDriver != null && existingDriver.getActiveSeasons() != null) {
                merged.addAll(existingDriver.getActiveSeasons());
            }

            if (parsedDriver.getActiveSeasons() != null) {
                merged.addAll(parsedDriver.getActiveSeasons());
            }

            merged.add(fetchedSeason);
            parsedDriver.setActiveSeasons(new ArrayList<>(merged));
        }
    }

    private void addActiveSeason(Driver driver, String season) {
        if (driver.getActiveSeasons() == null) {
            driver.setActiveSeasons(new ArrayList<>());
        }

        if (!driver.getActiveSeasons().contains(season)) {
            driver.getActiveSeasons().add(season);
        }
    }
}

package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.DriverStanding;
import no.f1fantasy.repository.DriverStandingRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Optional;

@Service
public class DriverStandingService {

    private static final Logger logger = LoggerFactory.getLogger(DriverStandingService.class);

    private final ErgastApiClient ergastApiClient;
    private final DriverStandingRepository driverStandingRepository;
    private final CacheStalenessService cacheStalenessService;
    private final ObjectMapper objectMapper;

    public DriverStandingService(
        ErgastApiClient ergastApiClient,
        DriverStandingRepository driverStandingRepository,
        CacheStalenessService cacheStalenessService,
        ObjectMapper objectMapper
    ) {
        this.ergastApiClient = ergastApiClient;
        this.driverStandingRepository = driverStandingRepository;
        this.cacheStalenessService = cacheStalenessService;
        this.objectMapper = objectMapper;
    }

    public List<DriverStanding> getDriverStandingsBySeason(String season) {
        List<DriverStanding> cachedLatest = sortByPosition(driverStandingRepository.findLatestBySeason(season));

        boolean shouldFetch = cacheStalenessService.shouldFetch(season, DataType.DRIVER_STANDINGS, CacheStalenessOptions.forStandings());
        if (!shouldFetch && !cachedLatest.isEmpty()) {
            return cachedLatest;
        }

        try {
            String payload = ergastApiClient.getJson("/" + season + "/driverstandings/");
            JsonNode standingsLists = objectMapper.readTree(payload)
                .path("MRData")
                .path("StandingsTable")
                .path("StandingsLists");

            if (!standingsLists.isArray() || standingsLists.isEmpty()) {
                return cachedLatest;
            }

            for (JsonNode standingsListNode : standingsLists) {
                String round = standingsListNode.path("round").asText();
                JsonNode driverStandingsNode = standingsListNode.path("DriverStandings");
                if (!driverStandingsNode.isArray()) {
                    continue;
                }

                List<DriverStanding> parsed = parseDriverStandings(driverStandingsNode, season, round);
                saveRoundBatch(season, round, parsed);
            }

            return sortByPosition(driverStandingRepository.findLatestBySeason(season));
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch driver standings for season {}, returning cached data", season, ex);
            return cachedLatest;
        }
    }

    public List<DriverStanding> getDriverStandingsByRound(String season, String round) {
        List<DriverStanding> cached = sortByPosition(driverStandingRepository.findBySeasonAndRound(season, round));

        try {
            String payload = ergastApiClient.getJson("/" + season + "/" + round + "/driverstandings/");
            JsonNode standingsLists = objectMapper.readTree(payload)
                .path("MRData")
                .path("StandingsTable")
                .path("StandingsLists");

            if (!standingsLists.isArray() || standingsLists.isEmpty()) {
                return cached;
            }

            JsonNode standingsListNode = standingsLists.get(0);
            JsonNode driverStandingsNode = standingsListNode.path("DriverStandings");
            if (!driverStandingsNode.isArray()) {
                return cached;
            }

            List<DriverStanding> parsed = parseDriverStandings(driverStandingsNode, season, round);
            saveRoundBatch(season, round, parsed);
            return sortByPosition(driverStandingRepository.findBySeasonAndRound(season, round));
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch driver standings for season {} round {}, returning cached data", season, round, ex);
            return cached;
        }
    }

    public Optional<DriverStanding> getDriverStandingByDriver(String season, String round, String driverId) {
        Optional<DriverStanding> cached = driverStandingRepository.findBySeasonAndRound(season, round)
            .stream()
            .filter(item -> driverId.equals(item.getDriverId()))
            .findFirst();

        if (cached.isPresent()) {
            return cached;
        }

        getDriverStandingsByRound(season, round);
        return driverStandingRepository.findBySeasonAndRound(season, round)
            .stream()
            .filter(item -> driverId.equals(item.getDriverId()))
            .findFirst();
    }

    public List<DriverStanding> getCachedStandings() {
        return driverStandingRepository.findAll();
    }

    private List<DriverStanding> parseDriverStandings(JsonNode standingsNode, String season, String round) {
        List<DriverStanding> parsed = new ArrayList<>();

        for (JsonNode node : standingsNode) {
            DriverStanding standing = new DriverStanding();
            standing.setSeason(season);
            standing.setRound(round);
            standing.setPosition(node.path("position").asText(null));
            standing.setPositionText(node.path("positionText").asText(null));
            standing.setPoints(node.path("points").asText(null));
            standing.setWins(node.path("wins").asText(null));

            JsonNode driverNode = node.path("Driver");
            if (!driverNode.isMissingNode()) {
                standing.setDriverId(driverNode.path("driverId").asText(null));
            }

            JsonNode constructorsNode = node.path("Constructors");
            if (constructorsNode.isArray() && !constructorsNode.isEmpty()) {
                standing.setConstructorId(constructorsNode.get(0).path("constructorId").asText(null));
            }

            if (standing.getDriverId() != null
                && standing.getConstructorId() != null
                && standing.getPosition() != null
                && standing.getPoints() != null) {
                parsed.add(standing);
            }
        }

        return parsed;
    }

    private void saveRoundBatch(String season, String round, List<DriverStanding> batch) {
        List<DriverStanding> existing = driverStandingRepository.findBySeasonAndRound(season, round);
        if (!existing.isEmpty()) {
            driverStandingRepository.deleteAll(existing);
        }

        if (!batch.isEmpty()) {
            driverStandingRepository.saveAll(batch);
        }
    }

    private List<DriverStanding> sortByPosition(List<DriverStanding> standings) {
        return standings.stream()
            .sorted(Comparator.comparingInt(item -> parseIntOrMax(item.getPosition())))
            .toList();
    }

    private int parseIntOrMax(String value) {
        if (value == null) {
            return Integer.MAX_VALUE;
        }

        try {
            return Integer.parseInt(value);
        } catch (NumberFormatException ex) {
            return Integer.MAX_VALUE;
        }
    }
}

package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Constructor;
import no.f1fantasy.repository.ConstructorRepository;
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
public class ConstructorService {

    private static final Logger logger = LoggerFactory.getLogger(ConstructorService.class);
    private static final String PAGINATION_KEY = "constructors";

    private final ErgastApiClient ergastApiClient;
    private final ConstructorRepository constructorRepository;
    private final PaginationStateTracker paginationStateTracker;
    private final ObjectMapper objectMapper;

    public ConstructorService(
        ErgastApiClient ergastApiClient,
        ConstructorRepository constructorRepository,
        PaginationStateTracker paginationStateTracker,
        ObjectMapper objectMapper
    ) {
        this.ergastApiClient = ergastApiClient;
        this.constructorRepository = constructorRepository;
        this.paginationStateTracker = paginationStateTracker;
        this.objectMapper = objectMapper;
    }

    public List<Constructor> getAllConstructors() {
        List<Constructor> cached = constructorRepository.findAll();
        if (!paginationStateTracker.shouldFetch(PAGINATION_KEY)) {
            return cached;
        }

        int limit = 30;
        int offset = paginationStateTracker.getNextOffset(PAGINATION_KEY, limit);
        int total = 0;

        try {
            do {
                String payload = ergastApiClient.getJson("/constructors/?offset=" + offset);
                JsonNode mrData = objectMapper.readTree(payload).path("MRData");
                total = mrData.path("total").asInt(0);
                JsonNode constructors = mrData.path("ConstructorTable").path("Constructors");

                if (!constructors.isArray() || constructors.isEmpty()) {
                    break;
                }

                List<Constructor> batch = new ArrayList<>();
                for (JsonNode constructorNode : constructors) {
                    batch.add(readConstructor(constructorNode));
                }

                constructorRepository.saveAll(batch);
                paginationStateTracker.updateState(PAGINATION_KEY, offset, total, limit);
                offset += limit;
            } while (offset < total);

            if (total == 0 || offset >= total) {
                paginationStateTracker.markComplete(PAGINATION_KEY);
            }

            return constructorRepository.findAll();
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch constructors from Ergast, returning cached data", ex);
            return cached;
        }
    }

    public List<Constructor> getConstructorsBySeason(String season) {
        List<Constructor> cached = constructorRepository.findAll();

        try {
            String payload = ergastApiClient.getJson("/" + season + "/constructors/");
            JsonNode constructors = objectMapper.readTree(payload)
                .path("MRData")
                .path("ConstructorTable")
                .path("Constructors");

            if (!constructors.isArray()) {
                return cached;
            }

            List<Constructor> parsed = new ArrayList<>();
            for (JsonNode constructorNode : constructors) {
                Constructor constructor = readConstructor(constructorNode);
                addActiveSeason(constructor, season);
                parsed.add(constructor);
            }

            mergeExistingActiveSeasons(parsed, season);
            constructorRepository.saveAll(parsed);
            return parsed;
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch constructors for season {}, returning cached data", season, ex);
            return cached;
        }
    }

    public Optional<Constructor> getConstructorById(String constructorId) {
        Optional<Constructor> constructor = constructorRepository.findByConstructorId(constructorId);
        if (constructor.isPresent()) {
            return constructor;
        }

        getAllConstructors();
        return constructorRepository.findByConstructorId(constructorId);
    }

    public List<Constructor> getCachedConstructors() {
        return constructorRepository.findAll();
    }

    public List<Constructor> getActiveConstructors(String season) {
        String targetSeason = season == null || season.isBlank() ? String.valueOf(Year.now().getValue()) : season;

        List<Constructor> active = constructorRepository.findAll().stream()
            .filter(constructor -> constructor.getActiveSeasons() != null && constructor.getActiveSeasons().contains(targetSeason))
            .toList();

        if (!active.isEmpty()) {
            return active;
        }

        getConstructorsBySeason(targetSeason);
        return constructorRepository.findAll().stream()
            .filter(constructor -> constructor.getActiveSeasons() != null && constructor.getActiveSeasons().contains(targetSeason))
            .toList();
    }

    private Constructor readConstructor(JsonNode node) {
        Constructor constructor = new Constructor();
        constructor.setConstructorId(node.path("constructorId").asText());
        constructor.setUrl(node.path("url").asText(null));
        constructor.setName(node.path("name").asText(null));
        constructor.setNationality(node.path("nationality").asText(null));
        return constructor;
    }

    private void mergeExistingActiveSeasons(List<Constructor> parsed, String fetchedSeason) {
        List<String> constructorIds = parsed.stream().map(Constructor::getConstructorId).collect(Collectors.toList());
        List<Constructor> existing = constructorRepository.findAllById(Objects.requireNonNull(constructorIds, "constructorIds"));

        for (Constructor parsedConstructor : parsed) {
            Set<String> merged = new LinkedHashSet<>();
            Constructor existingConstructor = existing.stream()
                .filter(item -> item.getConstructorId().equals(parsedConstructor.getConstructorId()))
                .findFirst()
                .orElse(null);

            if (existingConstructor != null && existingConstructor.getActiveSeasons() != null) {
                merged.addAll(existingConstructor.getActiveSeasons());
            }

            if (parsedConstructor.getActiveSeasons() != null) {
                merged.addAll(parsedConstructor.getActiveSeasons());
            }

            merged.add(fetchedSeason);
            parsedConstructor.setActiveSeasons(new ArrayList<>(merged));
        }
    }

    private void addActiveSeason(Constructor constructor, String season) {
        if (constructor.getActiveSeasons() == null) {
            constructor.setActiveSeasons(new ArrayList<>());
        }

        if (!constructor.getActiveSeasons().contains(season)) {
            constructor.getActiveSeasons().add(season);
        }
    }
}

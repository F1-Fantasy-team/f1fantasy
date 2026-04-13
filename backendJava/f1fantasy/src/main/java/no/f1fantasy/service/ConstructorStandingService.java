package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.ConstructorStanding;
import no.f1fantasy.repository.ConstructorStandingRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Optional;

@Service
public class ConstructorStandingService {

    private static final Logger logger = LoggerFactory.getLogger(ConstructorStandingService.class);

    private final ErgastApiClient ergastApiClient;
    private final ConstructorStandingRepository constructorStandingRepository;
    private final CacheStalenessService cacheStalenessService;
    private final ObjectMapper objectMapper;

    public ConstructorStandingService(
        ErgastApiClient ergastApiClient,
        ConstructorStandingRepository constructorStandingRepository,
        CacheStalenessService cacheStalenessService,
        ObjectMapper objectMapper
    ) {
        this.ergastApiClient = ergastApiClient;
        this.constructorStandingRepository = constructorStandingRepository;
        this.cacheStalenessService = cacheStalenessService;
        this.objectMapper = objectMapper;
    }

    public List<ConstructorStanding> getConstructorStandingsBySeason(String season) {
        List<ConstructorStanding> cachedLatest = sortByPosition(constructorStandingRepository.findLatestBySeason(season));

        boolean shouldFetch = cacheStalenessService.shouldFetch(season, DataType.CONSTRUCTOR_STANDINGS, CacheStalenessOptions.forStandings());
        if (!shouldFetch && !cachedLatest.isEmpty()) {
            return cachedLatest;
        }

        try {
            String payload = ergastApiClient.getJson("/" + season + "/constructorstandings/");
            JsonNode standingsLists = objectMapper.readTree(payload)
                .path("MRData")
                .path("StandingsTable")
                .path("StandingsLists");

            if (!standingsLists.isArray() || standingsLists.isEmpty()) {
                return cachedLatest;
            }

            for (JsonNode standingsListNode : standingsLists) {
                String round = standingsListNode.path("round").asText();
                JsonNode constructorStandingsNode = standingsListNode.path("ConstructorStandings");
                if (!constructorStandingsNode.isArray()) {
                    continue;
                }

                List<ConstructorStanding> parsed = parseConstructorStandings(constructorStandingsNode, season, round);
                saveRoundBatch(season, round, parsed);
            }

            return sortByPosition(constructorStandingRepository.findLatestBySeason(season));
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch constructor standings for season {}, returning cached data", season, ex);
            return cachedLatest;
        }
    }

    public List<ConstructorStanding> getConstructorStandingsByRound(String season, String round) {
        List<ConstructorStanding> cached = sortByPosition(constructorStandingRepository.findBySeasonAndRound(season, round));

        try {
            String payload = ergastApiClient.getJson("/" + season + "/" + round + "/constructorstandings/");
            JsonNode standingsLists = objectMapper.readTree(payload)
                .path("MRData")
                .path("StandingsTable")
                .path("StandingsLists");

            if (!standingsLists.isArray() || standingsLists.isEmpty()) {
                return cached;
            }

            JsonNode standingsListNode = standingsLists.get(0);
            JsonNode constructorStandingsNode = standingsListNode.path("ConstructorStandings");
            if (!constructorStandingsNode.isArray()) {
                return cached;
            }

            List<ConstructorStanding> parsed = parseConstructorStandings(constructorStandingsNode, season, round);
            saveRoundBatch(season, round, parsed);
            return sortByPosition(constructorStandingRepository.findBySeasonAndRound(season, round));
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch constructor standings for season {} round {}, returning cached data", season, round, ex);
            return cached;
        }
    }

    public Optional<ConstructorStanding> getConstructorStandingByConstructor(String season, String round, String constructorId) {
        Optional<ConstructorStanding> cached = constructorStandingRepository.findBySeasonAndRound(season, round)
            .stream()
            .filter(item -> constructorId.equals(item.getConstructorId()))
            .findFirst();

        if (cached.isPresent()) {
            return cached;
        }

        getConstructorStandingsByRound(season, round);
        return constructorStandingRepository.findBySeasonAndRound(season, round)
            .stream()
            .filter(item -> constructorId.equals(item.getConstructorId()))
            .findFirst();
    }

    public List<ConstructorStanding> getCachedStandings() {
        return constructorStandingRepository.findAll();
    }

    private List<ConstructorStanding> parseConstructorStandings(JsonNode standingsNode, String season, String round) {
        List<ConstructorStanding> parsed = new ArrayList<>();

        for (JsonNode node : standingsNode) {
            ConstructorStanding standing = new ConstructorStanding();
            standing.setSeason(season);
            standing.setRound(round);
            standing.setPosition(node.path("position").asText(null));
            standing.setPositionText(node.path("positionText").asText(null));
            standing.setPoints(node.path("points").asText(null));
            standing.setWins(node.path("wins").asText(null));

            JsonNode constructorNode = node.path("Constructor");
            if (!constructorNode.isMissingNode()) {
                standing.setConstructorId(constructorNode.path("constructorId").asText(null));
            }

            if (standing.getConstructorId() != null
                && standing.getPosition() != null
                && standing.getPoints() != null) {
                parsed.add(standing);
            }
        }

        return parsed;
    }

    private void saveRoundBatch(String season, String round, List<ConstructorStanding> batch) {
        List<ConstructorStanding> existing = constructorStandingRepository.findBySeasonAndRound(season, round);
        if (!existing.isEmpty()) {
            constructorStandingRepository.deleteAll(existing);
        }

        if (!batch.isEmpty()) {
            constructorStandingRepository.saveAll(batch);
        }
    }

    private List<ConstructorStanding> sortByPosition(List<ConstructorStanding> standings) {
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

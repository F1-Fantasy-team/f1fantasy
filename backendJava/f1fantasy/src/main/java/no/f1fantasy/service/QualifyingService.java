package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Qualifying;
import no.f1fantasy.repository.QualifyingRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

@Service
public class QualifyingService {

    private static final Logger logger = LoggerFactory.getLogger(QualifyingService.class);

    private final ErgastApiClient ergastApiClient;
    private final QualifyingRepository qualifyingRepository;
    private final CacheStalenessService cacheStalenessService;
    private final ObjectMapper objectMapper;

    public QualifyingService(
        ErgastApiClient ergastApiClient,
        QualifyingRepository qualifyingRepository,
        CacheStalenessService cacheStalenessService,
        ObjectMapper objectMapper
    ) {
        this.ergastApiClient = ergastApiClient;
        this.qualifyingRepository = qualifyingRepository;
        this.cacheStalenessService = cacheStalenessService;
        this.objectMapper = objectMapper;
    }

    public List<RaceWithQualifying> getQualifyingBySeason(String season) {
        List<Qualifying> cached = qualifyingRepository.findBySeason(season);

        boolean shouldFetch = cacheStalenessService.shouldFetch(season, DataType.QUALIFYING, CacheStalenessOptions.forQualifying());
        if (!shouldFetch && !cached.isEmpty()) {
            return buildRaceQualifying(cached);
        }

        try {
            String payload = ergastApiClient.getJson("/" + season + "/qualifying.json?limit=1000");
            JsonNode races = objectMapper.readTree(payload)
                .path("MRData")
                .path("RaceTable")
                .path("Races");

            if (!races.isArray()) {
                return buildRaceQualifying(cached);
            }

            for (JsonNode raceNode : races) {
                String round = raceNode.path("round").asText();
                JsonNode qualifyingNode = raceNode.path("QualifyingResults");
                if (!qualifyingNode.isArray()) {
                    continue;
                }

                List<Qualifying> batch = parseQualifying(qualifyingNode, season, round);
                saveRaceBatch(season, round, batch);
            }

            return buildRaceQualifying(qualifyingRepository.findBySeason(season));
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch qualifying for season {}, returning cached data", season, ex);
            return buildRaceQualifying(cached);
        }
    }

    public Optional<RaceWithQualifying> getQualifyingByRace(String season, String round) {
        List<Qualifying> cached = qualifyingRepository.findBySeasonAndRound(season, round);

        try {
            String payload = ergastApiClient.getJson("/" + season + "/" + round + "/qualifying/");
            JsonNode races = objectMapper.readTree(payload)
                .path("MRData")
                .path("RaceTable")
                .path("Races");

            if (!races.isArray() || races.isEmpty()) {
                return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithQualifying(season, round, cached));
            }

            JsonNode raceNode = races.get(0);
            JsonNode qualifyingNode = raceNode.path("QualifyingResults");
            if (!qualifyingNode.isArray()) {
                return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithQualifying(season, round, cached));
            }

            List<Qualifying> parsed = parseQualifying(qualifyingNode, season, round);
            saveRaceBatch(season, round, parsed);
            List<Qualifying> stored = qualifyingRepository.findBySeasonAndRound(season, round);
            return Optional.of(toRaceWithQualifying(season, round, stored));
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch qualifying for season {} round {}, returning cached data", season, round, ex);
            return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithQualifying(season, round, cached));
        }
    }

    public Optional<Qualifying> getQualifyingByDriver(String season, String round, String driverId) {
        Optional<Qualifying> cached = qualifyingRepository.findBySeasonAndRound(season, round)
            .stream()
            .filter(item -> driverId.equals(item.getDriverId()))
            .findFirst();

        if (cached.isPresent()) {
            return cached;
        }

        getQualifyingByRace(season, round);
        return qualifyingRepository.findBySeasonAndRound(season, round)
            .stream()
            .filter(item -> driverId.equals(item.getDriverId()))
            .findFirst();
    }

    public List<Qualifying> getCachedQualifying() {
        return qualifyingRepository.findAll();
    }

    private List<Qualifying> parseQualifying(JsonNode qualifyingNode, String season, String round) {
        List<Qualifying> parsed = new ArrayList<>();

        for (JsonNode node : qualifyingNode) {
            Qualifying qualifying = new Qualifying();
            qualifying.setSeason(season);
            qualifying.setRound(round);
            qualifying.setNumber(node.path("number").asText(null));
            qualifying.setPosition(node.path("position").asText(null));
            qualifying.setQ1(node.path("Q1").asText(null));
            qualifying.setQ2(node.path("Q2").asText(null));
            qualifying.setQ3(node.path("Q3").asText(null));

            JsonNode driverNode = node.path("Driver");
            if (!driverNode.isMissingNode()) {
                qualifying.setDriverId(driverNode.path("driverId").asText(null));
            }

            JsonNode constructorNode = node.path("Constructor");
            if (!constructorNode.isMissingNode()) {
                qualifying.setConstructorId(constructorNode.path("constructorId").asText(null));
            }

            if (qualifying.getDriverId() != null && qualifying.getConstructorId() != null) {
                parsed.add(qualifying);
            }
        }

        return parsed;
    }

    private void saveRaceBatch(String season, String round, List<Qualifying> batch) {
        List<Qualifying> existing = qualifyingRepository.findBySeasonAndRound(season, round);
        if (!existing.isEmpty()) {
            qualifyingRepository.deleteAll(existing);
        }

        if (!batch.isEmpty()) {
            qualifyingRepository.saveAll(batch);
        }
    }

    private List<RaceWithQualifying> buildRaceQualifying(List<Qualifying> qualifying) {
        return qualifying.stream()
            .collect(java.util.stream.Collectors.groupingBy(item -> item.getSeason() + "::" + item.getRound(),
                java.util.LinkedHashMap::new,
                java.util.stream.Collectors.toList()))
            .values()
            .stream()
            .map(group -> toRaceWithQualifying(group.get(0).getSeason(), group.get(0).getRound(), group))
            .toList();
    }

    private RaceWithQualifying toRaceWithQualifying(String season, String round, List<Qualifying> qualifying) {
        RaceWithQualifying raceWithQualifying = new RaceWithQualifying();
        raceWithQualifying.setSeason(season);
        raceWithQualifying.setRound(round);
        raceWithQualifying.setQualifyingResults(qualifying);
        return raceWithQualifying;
    }
}

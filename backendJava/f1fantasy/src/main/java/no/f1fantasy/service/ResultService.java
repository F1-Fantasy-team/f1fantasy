package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.AverageSpeed;
import no.f1fantasy.entity.FastestLap;
import no.f1fantasy.entity.LapTime;
import no.f1fantasy.entity.Result;
import no.f1fantasy.entity.ResultTime;
import no.f1fantasy.repository.ResultRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

@Service
public class ResultService {

    private static final Logger logger = LoggerFactory.getLogger(ResultService.class);

    private final ErgastApiClient ergastApiClient;
    private final ResultRepository resultRepository;
    private final CacheStalenessService cacheStalenessService;
    private final ObjectMapper objectMapper;

    public ResultService(
        ErgastApiClient ergastApiClient,
        ResultRepository resultRepository,
        CacheStalenessService cacheStalenessService,
        ObjectMapper objectMapper
    ) {
        this.ergastApiClient = ergastApiClient;
        this.resultRepository = resultRepository;
        this.cacheStalenessService = cacheStalenessService;
        this.objectMapper = objectMapper;
    }

    public List<RaceWithResults> getResultsBySeason(String season) {
        List<Result> cached = resultRepository.findBySeason(season);

        boolean shouldFetch = cacheStalenessService.shouldFetch(season, DataType.RESULTS, CacheStalenessOptions.forResults());
        if (!shouldFetch && !cached.isEmpty()) {
            return buildRaceResults(cached, false);
        }

        try {
            String payload = ergastApiClient.getJson("/" + season + "/results/?limit=1000");
            JsonNode races = objectMapper.readTree(payload)
                .path("MRData")
                .path("RaceTable")
                .path("Races");

            if (!races.isArray()) {
                return buildRaceResults(cached, false);
            }

            for (JsonNode raceNode : races) {
                String round = raceNode.path("round").asText();
                JsonNode resultsNode = raceNode.path("Results");
                if (!resultsNode.isArray()) {
                    continue;
                }

                List<Result> batch = parseResults(resultsNode, season, round, false);
                saveRaceBatch(season, round, false, batch);
            }

            return buildRaceResults(resultRepository.findBySeason(season), false);
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch results for season {}, returning cached data", season, ex);
            return buildRaceResults(cached, false);
        }
    }

    public Optional<RaceWithResults> getResultsByRace(String season, String round) {
        List<Result> cached = resultRepository.findBySeasonAndRoundAndIsSprint(season, round, false);

        try {
            String payload = ergastApiClient.getJson("/" + season + "/" + round + "/results/");
            JsonNode races = objectMapper.readTree(payload)
                .path("MRData")
                .path("RaceTable")
                .path("Races");

            if (!races.isArray() || races.isEmpty()) {
                return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithResults(season, round, cached, false));
            }

            JsonNode raceNode = races.get(0);
            JsonNode resultsNode = raceNode.path("Results");
            if (!resultsNode.isArray()) {
                return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithResults(season, round, cached, false));
            }

            List<Result> parsed = parseResults(resultsNode, season, round, false);
            saveRaceBatch(season, round, false, parsed);
            List<Result> stored = resultRepository.findBySeasonAndRoundAndIsSprint(season, round, false);
            return Optional.of(toRaceWithResults(season, round, stored, false));
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch results for season {} round {}, returning cached data", season, round, ex);
            return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithResults(season, round, cached, false));
        }
    }

    public Optional<Result> getResultByDriver(String season, String round, String driverId) {
        Optional<Result> cached = resultRepository.findBySeasonAndRoundAndDriverId(season, round, driverId);
        if (cached.isPresent()) {
            return cached;
        }

        getResultsByRace(season, round);
        return resultRepository.findBySeasonAndRoundAndDriverId(season, round, driverId);
    }

    public List<Result> getCachedResults() {
        return resultRepository.findAll();
    }

    public Optional<Integer> getLatestRoundWithResults(String season) {
        return resultRepository.findBySeason(season)
            .stream()
            .filter(result -> !result.isSprint())
            .map(Result::getRound)
            .map(this::parseRound)
            .flatMap(Optional::stream)
            .max(Integer::compareTo);
    }

    public List<RaceWithResults> getSprintResultsBySeason(String season) {
        List<Result> cached = resultRepository.findBySeason(season).stream().filter(Result::isSprint).toList();

        try {
            String payload = ergastApiClient.getJson("/" + season + "/sprint/");
            JsonNode races = objectMapper.readTree(payload)
                .path("MRData")
                .path("RaceTable")
                .path("Races");

            if (!races.isArray()) {
                return buildRaceResults(cached, true);
            }

            for (JsonNode raceNode : races) {
                String round = raceNode.path("round").asText();
                JsonNode resultsNode = raceNode.path("SprintResults");
                if (!resultsNode.isArray()) {
                    continue;
                }

                List<Result> batch = parseResults(resultsNode, season, round, true);
                saveRaceBatch(season, round, true, batch);
            }

            List<Result> stored = resultRepository.findBySeason(season).stream().filter(Result::isSprint).toList();
            return buildRaceResults(stored, true);
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch sprint results for season {}, returning cached data", season, ex);
            return buildRaceResults(cached, true);
        }
    }

    public Optional<RaceWithResults> getSprintResultsByRace(String season, String round) {
        List<Result> cached = resultRepository.findBySeasonAndRoundAndIsSprint(season, round, true);

        try {
            String payload = ergastApiClient.getJson("/" + season + "/" + round + "/sprint/");
            JsonNode races = objectMapper.readTree(payload)
                .path("MRData")
                .path("RaceTable")
                .path("Races");

            if (!races.isArray() || races.isEmpty()) {
                return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithResults(season, round, cached, true));
            }

            JsonNode raceNode = races.get(0);
            JsonNode resultsNode = raceNode.path("SprintResults");
            if (!resultsNode.isArray()) {
                return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithResults(season, round, cached, true));
            }

            List<Result> parsed = parseResults(resultsNode, season, round, true);
            saveRaceBatch(season, round, true, parsed);
            List<Result> stored = resultRepository.findBySeasonAndRoundAndIsSprint(season, round, true);
            return Optional.of(toRaceWithResults(season, round, stored, true));
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch sprint results for season {} round {}, returning cached data", season, round, ex);
            return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithResults(season, round, cached, true));
        }
    }

    private List<Result> parseResults(JsonNode resultsNode, String season, String round, boolean isSprint) {
        List<Result> parsed = new ArrayList<>();

        for (JsonNode resultNode : resultsNode) {
            Result result = new Result();
            result.setSeason(season);
            result.setRound(round);
            result.setSprint(isSprint);
            result.setNumber(resultNode.path("number").asText(null));
            result.setPosition(resultNode.path("position").asText(null));
            result.setPositionText(resultNode.path("positionText").asText(null));
            result.setPoints(resultNode.path("points").asText(null));
            result.setGrid(resultNode.path("grid").asText(null));
            result.setLaps(resultNode.path("laps").asText(null));
            result.setStatus(resultNode.path("status").asText(null));

            JsonNode driverNode = resultNode.path("Driver");
            if (!driverNode.isMissingNode()) {
                result.setDriverId(driverNode.path("driverId").asText(null));
            }

            JsonNode constructorNode = resultNode.path("Constructor");
            if (!constructorNode.isMissingNode()) {
                result.setConstructorId(constructorNode.path("constructorId").asText(null));
            }

            JsonNode timeNode = resultNode.path("Time");
            if (!timeNode.isMissingNode() && !timeNode.isNull()) {
                ResultTime resultTime = new ResultTime();
                resultTime.setMillis(timeNode.path("millis").asText(null));
                resultTime.setTime(timeNode.path("time").asText(null));
                result.setResultTime(resultTime);
            }

            JsonNode fastestLapNode = resultNode.path("FastestLap");
            if (!fastestLapNode.isMissingNode() && !fastestLapNode.isNull()) {
                FastestLap fastestLap = new FastestLap();
                fastestLap.setRank(fastestLapNode.path("rank").asText(null));
                fastestLap.setLap(fastestLapNode.path("lap").asText(null));

                JsonNode fastestLapTimeNode = fastestLapNode.path("Time");
                if (!fastestLapTimeNode.isMissingNode() && !fastestLapTimeNode.isNull()) {
                    LapTime lapTime = new LapTime();
                    lapTime.setTime(fastestLapTimeNode.path("time").asText(null));
                    fastestLap.setLapTime(lapTime);
                }

                JsonNode averageSpeedNode = fastestLapNode.path("AverageSpeed");
                if (!averageSpeedNode.isMissingNode() && !averageSpeedNode.isNull()) {
                    AverageSpeed averageSpeed = new AverageSpeed();
                    averageSpeed.setUnits(averageSpeedNode.path("units").asText(null));
                    averageSpeed.setSpeed(averageSpeedNode.path("speed").asText(null));
                    fastestLap.setAverageSpeed(averageSpeed);
                }

                result.setFastestLap(fastestLap);
            }

            if (result.getDriverId() != null && result.getConstructorId() != null) {
                parsed.add(result);
            }
        }

        return parsed;
    }

    private void saveRaceBatch(String season, String round, boolean sprint, List<Result> batch) {
        List<Result> existing = resultRepository.findBySeasonAndRoundAndIsSprint(season, round, sprint);
        if (!existing.isEmpty()) {
            resultRepository.deleteAll(existing);
        }

        if (!batch.isEmpty()) {
            resultRepository.saveAll(batch);
        }
    }

    private List<RaceWithResults> buildRaceResults(List<Result> results, boolean sprint) {
        return results.stream()
            .collect(java.util.stream.Collectors.groupingBy(result -> result.getSeason() + "::" + result.getRound(),
                java.util.LinkedHashMap::new,
                java.util.stream.Collectors.toList()))
            .values()
            .stream()
            .map(group -> toRaceWithResults(group.get(0).getSeason(), group.get(0).getRound(), group, sprint))
            .toList();
    }

    private RaceWithResults toRaceWithResults(String season, String round, List<Result> results, boolean sprint) {
        RaceWithResults raceWithResults = new RaceWithResults();
        raceWithResults.setSeason(season);
        raceWithResults.setRound(round);
        if (sprint) {
            raceWithResults.setSprintResults(results);
        } else {
            raceWithResults.setResults(results);
        }
        return raceWithResults;
    }

    private Optional<Integer> parseRound(String round) {
        if (round == null) {
            return Optional.empty();
        }

        try {
            return Optional.of(Integer.valueOf(round));
        } catch (NumberFormatException ex) {
            return Optional.empty();
        }
    }
}

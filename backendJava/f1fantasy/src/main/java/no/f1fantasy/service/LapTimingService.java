package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.LapTiming;
import no.f1fantasy.repository.LapTimingRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Optional;

@Service
public class LapTimingService {

    private static final Logger logger = LoggerFactory.getLogger(LapTimingService.class);

    private final ErgastApiClient ergastApiClient;
    private final LapTimingRepository lapTimingRepository;
    private final ObjectMapper objectMapper;

    public LapTimingService(
        ErgastApiClient ergastApiClient,
        LapTimingRepository lapTimingRepository,
        ObjectMapper objectMapper
    ) {
        this.ergastApiClient = ergastApiClient;
        this.lapTimingRepository = lapTimingRepository;
        this.objectMapper = objectMapper;
    }

    public Optional<RaceWithLaps> getLapsByRace(String season, String round) {
        List<LapTiming> cached = lapTimingRepository.findBySeasonAndRound(season, round);

        try {
            String payload = ergastApiClient.getJson("/" + season + "/" + round + "/laps/");
            JsonNode races = objectMapper.readTree(payload)
                .path("MRData")
                .path("RaceTable")
                .path("Races");

            if (!races.isArray() || races.isEmpty()) {
                return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithLaps(season, round, cached));
            }

            JsonNode raceNode = races.get(0);
            JsonNode lapsNode = raceNode.path("Laps");
            if (!lapsNode.isArray()) {
                return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithLaps(season, round, cached));
            }

            List<LapTiming> parsed = parseLapTimings(lapsNode, season, round);
            List<LapTiming> existing = lapTimingRepository.findBySeasonAndRound(season, round);
            if (!existing.isEmpty()) {
                lapTimingRepository.deleteAll(existing);
            }
            if (!parsed.isEmpty()) {
                lapTimingRepository.saveAll(parsed);
            }

            List<LapTiming> stored = lapTimingRepository.findBySeasonAndRound(season, round);
            return Optional.of(toRaceWithLaps(season, round, stored));
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch lap timings for season {} round {}, returning cached data", season, round, ex);
            return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithLaps(season, round, cached));
        }
    }

    public Optional<RaceWithLaps.Lap> getLapByNumber(String season, String round, String lapNumber) {
        List<LapTiming> cached = filterByLapNumber(lapTimingRepository.findBySeasonAndRound(season, round), lapNumber);
        if (!cached.isEmpty()) {
            return Optional.of(toLap(lapNumber, cached));
        }

        getLapsByRace(season, round);
        List<LapTiming> refreshed = filterByLapNumber(lapTimingRepository.findBySeasonAndRound(season, round), lapNumber);
        return refreshed.isEmpty() ? Optional.empty() : Optional.of(toLap(lapNumber, refreshed));
    }

    public List<LapTiming> getLapsByDriver(String season, String round, String driverId) {
        List<LapTiming> cached = lapTimingRepository.findBySeasonAndRound(season, round)
            .stream()
            .filter(item -> driverId.equals(item.getDriverId()))
            .sorted(lapThenPositionComparator())
            .toList();

        if (!cached.isEmpty()) {
            return cached;
        }

        getLapsByRace(season, round);
        return lapTimingRepository.findBySeasonAndRound(season, round)
            .stream()
            .filter(item -> driverId.equals(item.getDriverId()))
            .sorted(lapThenPositionComparator())
            .toList();
    }

    public List<LapTiming> getCachedLaps() {
        return lapTimingRepository.findAll();
    }

    private List<LapTiming> parseLapTimings(JsonNode lapsNode, String season, String round) {
        List<LapTiming> parsed = new ArrayList<>();

        for (JsonNode lapNode : lapsNode) {
            String lapNumber = lapNode.path("number").asText(null);
            JsonNode timingsNode = lapNode.path("Timings");
            if (lapNumber == null || !timingsNode.isArray()) {
                continue;
            }

            for (JsonNode timingNode : timingsNode) {
                LapTiming lapTiming = new LapTiming();
                lapTiming.setSeason(season);
                lapTiming.setRound(round);
                lapTiming.setLapNumber(lapNumber);
                lapTiming.setDriverId(timingNode.path("driverId").asText(null));
                lapTiming.setPosition(timingNode.path("position").asText(null));
                lapTiming.setTime(timingNode.path("time").asText(null));

                if (lapTiming.getDriverId() != null && lapTiming.getPosition() != null) {
                    parsed.add(lapTiming);
                }
            }
        }

        return parsed;
    }

    private RaceWithLaps toRaceWithLaps(String season, String round, List<LapTiming> timings) {
        RaceWithLaps raceWithLaps = new RaceWithLaps();
        raceWithLaps.setSeason(season);
        raceWithLaps.setRound(round);

        List<RaceWithLaps.Lap> laps = timings.stream()
            .collect(java.util.stream.Collectors.groupingBy(
                LapTiming::getLapNumber,
                java.util.LinkedHashMap::new,
                java.util.stream.Collectors.toList()))
            .entrySet()
            .stream()
            .sorted(Comparator.comparingInt(entry -> parseIntOrMax(entry.getKey())))
            .map(entry -> toLap(entry.getKey(), entry.getValue()))
            .toList();

        raceWithLaps.setLaps(laps);
        return raceWithLaps;
    }

    private RaceWithLaps.Lap toLap(String lapNumber, List<LapTiming> timings) {
        RaceWithLaps.Lap lap = new RaceWithLaps.Lap();
        lap.setNumber(lapNumber);
        lap.setTimings(timings.stream().sorted(positionComparator()).toList());
        return lap;
    }

    private List<LapTiming> filterByLapNumber(List<LapTiming> timings, String lapNumber) {
        return timings.stream()
            .filter(item -> lapNumber.equals(item.getLapNumber()))
            .sorted(positionComparator())
            .toList();
    }

    private Comparator<LapTiming> lapThenPositionComparator() {
        return Comparator
            .comparingInt((LapTiming item) -> parseIntOrMax(item.getLapNumber()))
            .thenComparingInt(item -> parseIntOrMax(item.getPosition()));
    }

    private Comparator<LapTiming> positionComparator() {
        return Comparator.comparingInt(item -> parseIntOrMax(item.getPosition()));
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

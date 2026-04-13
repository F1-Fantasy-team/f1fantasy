package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.PitStop;
import no.f1fantasy.repository.PitStopRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

@Service
public class PitStopService {

    private static final Logger logger = LoggerFactory.getLogger(PitStopService.class);

    private final ErgastApiClient ergastApiClient;
    private final PitStopRepository pitStopRepository;
    private final ObjectMapper objectMapper;

    public PitStopService(
        ErgastApiClient ergastApiClient,
        PitStopRepository pitStopRepository,
        ObjectMapper objectMapper
    ) {
        this.ergastApiClient = ergastApiClient;
        this.pitStopRepository = pitStopRepository;
        this.objectMapper = objectMapper;
    }

    public Optional<RaceWithPitStops> getPitStopsByRace(String season, String round) {
        List<PitStop> cached = pitStopRepository.findBySeasonAndRound(season, round);

        try {
            String payload = ergastApiClient.getJson("/" + season + "/" + round + "/pitstops/");
            JsonNode races = objectMapper.readTree(payload)
                .path("MRData")
                .path("RaceTable")
                .path("Races");

            if (!races.isArray() || races.isEmpty()) {
                return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithPitStops(season, round, cached));
            }

            JsonNode raceNode = races.get(0);
            JsonNode pitStopsNode = raceNode.path("PitStops");
            if (!pitStopsNode.isArray()) {
                return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithPitStops(season, round, cached));
            }

            List<PitStop> parsed = parsePitStops(pitStopsNode, season, round);
            List<PitStop> existing = pitStopRepository.findBySeasonAndRound(season, round);
            if (!existing.isEmpty()) {
                pitStopRepository.deleteAll(existing);
            }
            if (!parsed.isEmpty()) {
                pitStopRepository.saveAll(parsed);
            }

            List<PitStop> stored = pitStopRepository.findBySeasonAndRound(season, round);
            return Optional.of(toRaceWithPitStops(season, round, stored));
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch pit stops for season {} round {}, returning cached data", season, round, ex);
            return cached.isEmpty() ? Optional.empty() : Optional.of(toRaceWithPitStops(season, round, cached));
        }
    }

    public List<PitStop> getPitStopsByDriver(String season, String round, String driverId) {
        List<PitStop> cached = pitStopRepository.findBySeasonAndRound(season, round)
            .stream()
            .filter(item -> driverId.equals(item.getDriverId()))
            .toList();

        if (!cached.isEmpty()) {
            return cached;
        }

        getPitStopsByRace(season, round);
        return pitStopRepository.findBySeasonAndRound(season, round)
            .stream()
            .filter(item -> driverId.equals(item.getDriverId()))
            .toList();
    }

    public List<PitStop> getCachedPitStops() {
        return pitStopRepository.findAll();
    }

    private List<PitStop> parsePitStops(JsonNode pitStopsNode, String season, String round) {
        List<PitStop> parsed = new ArrayList<>();

        for (JsonNode node : pitStopsNode) {
            PitStop pitStop = new PitStop();
            pitStop.setSeason(season);
            pitStop.setRound(round);
            pitStop.setDriverId(node.path("driverId").asText(null));
            pitStop.setLap(node.path("lap").asText(null));
            pitStop.setStop(node.path("stop").asText(null));
            pitStop.setTime(node.path("time").asText(null));
            pitStop.setDuration(node.path("duration").asText(null));

            if (pitStop.getDriverId() != null && pitStop.getLap() != null && pitStop.getStop() != null) {
                parsed.add(pitStop);
            }
        }

        return parsed;
    }

    private RaceWithPitStops toRaceWithPitStops(String season, String round, List<PitStop> pitStops) {
        RaceWithPitStops raceWithPitStops = new RaceWithPitStops();
        raceWithPitStops.setSeason(season);
        raceWithPitStops.setRound(round);
        raceWithPitStops.setPitStops(pitStops);
        return raceWithPitStops;
    }
}

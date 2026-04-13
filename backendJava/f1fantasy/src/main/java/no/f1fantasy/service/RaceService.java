package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Race;
import no.f1fantasy.entity.Session;
import no.f1fantasy.repository.RaceRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

@Service
public class RaceService {

    private static final Logger logger = LoggerFactory.getLogger(RaceService.class);

    private final ErgastApiClient ergastApiClient;
    private final RaceRepository raceRepository;
    private final CacheStalenessService cacheStalenessService;
    private final ObjectMapper objectMapper;

    public RaceService(
        ErgastApiClient ergastApiClient,
        RaceRepository raceRepository,
        CacheStalenessService cacheStalenessService,
        ObjectMapper objectMapper
    ) {
        this.ergastApiClient = ergastApiClient;
        this.raceRepository = raceRepository;
        this.cacheStalenessService = cacheStalenessService;
        this.objectMapper = objectMapper;
    }

    public List<Race> getRacesForSeason(String season) {
        List<Race> cached = raceRepository.findBySeason(season);

        CacheStalenessOptions options = new CacheStalenessOptions();
        options.setCheckRaceSchedule(false);

        boolean shouldFetch = cacheStalenessService.shouldFetch(season, DataType.RACES, options);
        if (!shouldFetch && !cached.isEmpty()) {
            return cached;
        }

        try {
            String payload = ergastApiClient.getJson("/" + season + "/races/");
            JsonNode racesNode = objectMapper.readTree(payload)
                .path("MRData")
                .path("RaceTable")
                .path("Races");

            if (!racesNode.isArray()) {
                return cached;
            }

            List<Race> parsed = new ArrayList<>();
            for (JsonNode raceNode : racesNode) {
                Race race = new Race();
                race.setSeason(raceNode.path("season").asText());
                race.setRound(raceNode.path("round").asText());
                race.setRaceName(raceNode.path("raceName").asText(null));
                race.setUrl(raceNode.path("url").asText(null));
                race.setDate(raceNode.path("date").asText(null));
                race.setTime(raceNode.path("time").asText(null));
                race.setFirstPractice(readSession(raceNode.path("FirstPractice")));
                race.setSecondPractice(readSession(raceNode.path("SecondPractice")));
                race.setThirdPractice(readSession(raceNode.path("ThirdPractice")));
                race.setQualifying(readSession(raceNode.path("Qualifying")));
                race.setSprint(readSession(raceNode.path("Sprint")));
                race.setSprintQualifying(readSession(raceNode.path("SprintQualifying")));
                parsed.add(race);
            }

            raceRepository.saveAll(parsed);
            return raceRepository.findBySeason(season);
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch races for season {}, returning cached data", season, ex);
            return cached;
        }
    }

    public Optional<Race> getRaceByRound(String season, String round) {
        return raceRepository.findBySeasonAndRound(season, round);
    }

    public List<Race> getAllRaces() {
        return raceRepository.findAll();
    }

    private Session readSession(JsonNode node) {
        if (node == null || node.isMissingNode() || node.isNull()) {
            return null;
        }

        Session session = new Session();
        session.setDate(node.path("date").asText(null));
        session.setTime(node.path("time").asText(null));
        return session;
    }
}

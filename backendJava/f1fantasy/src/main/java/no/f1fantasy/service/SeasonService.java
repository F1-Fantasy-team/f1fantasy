package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Season;
import no.f1fantasy.repository.SeasonRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

@Service
public class SeasonService {

    private static final Logger logger = LoggerFactory.getLogger(SeasonService.class);
    private static final String PAGINATION_KEY = "seasons";

    private final ErgastApiClient ergastApiClient;
    private final SeasonRepository seasonRepository;
    private final PaginationStateTracker paginationStateTracker;
    private final ObjectMapper objectMapper;

    public SeasonService(
        ErgastApiClient ergastApiClient,
        SeasonRepository seasonRepository,
        PaginationStateTracker paginationStateTracker,
        ObjectMapper objectMapper
    ) {
        this.ergastApiClient = ergastApiClient;
        this.seasonRepository = seasonRepository;
        this.paginationStateTracker = paginationStateTracker;
        this.objectMapper = objectMapper;
    }

    public List<Season> getAllSeasons() {
        List<Season> cached = seasonRepository.findAll();
        if (!paginationStateTracker.shouldFetch(PAGINATION_KEY)) {
            return cached;
        }

        int limit = 30;
        int offset = paginationStateTracker.getNextOffset(PAGINATION_KEY, limit);
        int total = 0;

        try {
            do {
                String payload = ergastApiClient.getJson("/seasons/?offset=" + offset);
                JsonNode mrData = objectMapper.readTree(payload).path("MRData");
                total = mrData.path("total").asInt(0);
                JsonNode seasons = mrData.path("SeasonTable").path("Seasons");

                if (!seasons.isArray() || seasons.isEmpty()) {
                    break;
                }

                List<Season> batch = new ArrayList<>();
                for (JsonNode seasonNode : seasons) {
                    Season season = new Season();
                    season.setYear(seasonNode.path("season").asText());
                    season.setUrl(seasonNode.path("url").asText(null));
                    batch.add(season);
                }

                seasonRepository.saveAll(batch);
                paginationStateTracker.updateState(PAGINATION_KEY, offset, total, limit);
                offset += limit;
            } while (offset < total);

            if (total == 0 || offset >= total) {
                paginationStateTracker.markComplete(PAGINATION_KEY);
            }

            return seasonRepository.findAll();
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch seasons from Ergast, returning cached data", ex);
            return cached;
        }
    }

    public Optional<Season> getSeasonByYear(String year) {
        Optional<Season> season = seasonRepository.findByYear(year);
        if (season.isPresent()) {
            return season;
        }

        getAllSeasons();
        return seasonRepository.findByYear(year);
    }

    public List<Season> getCachedSeasons() {
        return seasonRepository.findAll();
    }
}

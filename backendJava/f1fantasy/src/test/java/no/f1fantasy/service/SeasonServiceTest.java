package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Season;
import no.f1fantasy.repository.SeasonRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.OffsetDateTime;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class SeasonServiceTest {

    @Mock
    private ErgastApiClient ergastApiClient;

    @Mock
    private SeasonRepository seasonRepository;

    private final PaginationStateTracker paginationStateTracker = new PaginationStateTracker();

    @InjectMocks
    @SuppressWarnings("unused")
    private SeasonService seasonService;

    @Test
    void shouldUseCachedSeasonsWhenStateIsFresh() {
        paginationStateTracker.getState("seasons").setComplete(true);
        paginationStateTracker.getState("seasons").setLastUpdate(OffsetDateTime.now());

        Season cached = new Season();
        cached.setYear("2024");
        when(seasonRepository.findAll()).thenReturn(List.of(cached));

        SeasonService service = new SeasonService(ergastApiClient, seasonRepository, paginationStateTracker, new ObjectMapper());
        List<Season> seasons = service.getAllSeasons();

        assertThat(seasons).hasSize(1);
        verifyNoInteractions(ergastApiClient);
    }

    @Test
    @SuppressWarnings("null")
    void shouldFetchAndPersistWhenStale() {
        String payload = """
            {
              \"MRData\": {
                \"total\": \"2\",
                \"SeasonTable\": {
                  \"Seasons\": [
                    {\"season\": \"2024\", \"url\": \"u1\"},
                    {\"season\": \"2025\", \"url\": \"u2\"}
                  ]
                }
              }
            }
            """;

        when(seasonRepository.findAll())
            .thenReturn(List.of())
            .thenReturn(List.of(new Season(), new Season()));
        when(ergastApiClient.getJson(anyString())).thenReturn(payload);

        SeasonService service = new SeasonService(ergastApiClient, seasonRepository, paginationStateTracker, new ObjectMapper());
        List<Season> seasons = service.getAllSeasons();

        assertThat(seasons).hasSize(2);
        verify(seasonRepository, times(1)).saveAll(anyList());
        verify(ergastApiClient, times(1)).getJson("/seasons/?offset=0");
    }

    @Test
    void shouldFindByYearAfterRefresh() {
        Season season = new Season();
        season.setYear("2026");

        when(seasonRepository.findByYear("2026"))
            .thenReturn(Optional.empty())
            .thenReturn(Optional.of(season));
        when(seasonRepository.findAll())
            .thenReturn(List.of())
            .thenReturn(List.of(season));
        when(ergastApiClient.getJson(anyString())).thenReturn("""
            {\"MRData\":{\"total\":\"1\",\"SeasonTable\":{\"Seasons\":[{\"season\":\"2026\",\"url\":\"u\"}]}}}
            """);

        SeasonService service = new SeasonService(ergastApiClient, seasonRepository, paginationStateTracker, new ObjectMapper());
        Optional<Season> found = service.getSeasonByYear("2026");

        assertThat(found).isPresent();
        assertThat(found.get().getYear()).isEqualTo("2026");
    }

      @Test
      void shouldFallbackToCachedSeasonsWhenApiFails() {
        Season cached = new Season();
        cached.setYear("2024");

        when(seasonRepository.findAll()).thenReturn(List.of(cached));
        when(ergastApiClient.getJson(anyString())).thenThrow(new RuntimeException("boom"));

        SeasonService service = new SeasonService(ergastApiClient, seasonRepository, new PaginationStateTracker(), new ObjectMapper());
        List<Season> seasons = service.getAllSeasons();

        assertThat(seasons).hasSize(1);
        assertThat(seasons.getFirst().getYear()).isEqualTo("2024");
      }
}

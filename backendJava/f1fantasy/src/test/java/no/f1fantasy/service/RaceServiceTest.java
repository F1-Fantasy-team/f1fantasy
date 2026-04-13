package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Race;
import no.f1fantasy.repository.RaceRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class RaceServiceTest {

    @Mock
    private ErgastApiClient ergastApiClient;

    @Mock
    private RaceRepository raceRepository;

    @Mock
    private CacheStalenessService cacheStalenessService;

    @Test
    void shouldUseCacheWhenNotStale() {
        Race cached = new Race();
        cached.setSeason("2025");
        cached.setRound("1");

        when(raceRepository.findBySeason("2025")).thenReturn(List.of(cached));
        when(cacheStalenessService.shouldFetch(eq("2025"), eq(DataType.RACES), any(CacheStalenessOptions.class)))
            .thenReturn(false);

        RaceService service = new RaceService(ergastApiClient, raceRepository, cacheStalenessService, new ObjectMapper());
        List<Race> races = service.getRacesForSeason("2025");

        assertThat(races).hasSize(1);
        verifyNoInteractions(ergastApiClient);
    }

    @Test
    @SuppressWarnings("null")
    void shouldFetchAndPersistWhenStale() {
        String payload = """
            {
              \"MRData\": {
                \"RaceTable\": {
                  \"Races\": [
                    {
                      \"season\": \"2025\",
                      \"round\": \"1\",
                      \"raceName\": \"Bahrain Grand Prix\",
                      \"url\": \"u\",
                      \"date\": \"2025-03-02\",
                      \"time\": \"15:00:00Z\"
                    }
                  ]
                }
              }
            }
            """;

        Race persisted = new Race();
        persisted.setSeason("2025");
        persisted.setRound("1");

        when(raceRepository.findBySeason("2025"))
            .thenReturn(List.of())
            .thenReturn(List.of(persisted));
        when(cacheStalenessService.shouldFetch(eq("2025"), eq(DataType.RACES), any(CacheStalenessOptions.class)))
            .thenReturn(true);
        when(ergastApiClient.getJson("/2025/races/")).thenReturn(payload);

        RaceService service = new RaceService(ergastApiClient, raceRepository, cacheStalenessService, new ObjectMapper());
        List<Race> races = service.getRacesForSeason("2025");

        assertThat(races).hasSize(1);
        verify(raceRepository, times(1)).saveAll(anyList());
    }

    @Test
    void shouldFallbackToCacheWhenApiFails() {
        Race cached = new Race();
        cached.setSeason("2025");
        cached.setRound("2");

        when(raceRepository.findBySeason("2025")).thenReturn(List.of(cached));
        when(cacheStalenessService.shouldFetch(eq("2025"), eq(DataType.RACES), any(CacheStalenessOptions.class)))
            .thenReturn(true);
        when(ergastApiClient.getJson("/2025/races/")).thenThrow(new RuntimeException("boom"));

        RaceService service = new RaceService(ergastApiClient, raceRepository, cacheStalenessService, new ObjectMapper());
        List<Race> races = service.getRacesForSeason("2025");

        assertThat(races).hasSize(1);
        assertThat(races.getFirst().getRound()).isEqualTo("2");
    }
}

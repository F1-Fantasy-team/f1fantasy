package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Qualifying;
import no.f1fantasy.repository.QualifyingRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@SuppressWarnings("null")
class QualifyingServiceTest {

    @Mock
    private ErgastApiClient ergastApiClient;

    @Mock
    private QualifyingRepository qualifyingRepository;

    @Mock
    private CacheStalenessService cacheStalenessService;

    @Test
    void shouldUseCacheWhenNotStale() {
        Qualifying cached = new Qualifying();
        cached.setSeason("2025");
        cached.setRound("1");
        cached.setDriverId("hamilton");
        cached.setConstructorId("mercedes");

        when(qualifyingRepository.findBySeason("2025")).thenReturn(List.of(cached));
        when(cacheStalenessService.shouldFetch(eq("2025"), eq(DataType.QUALIFYING), any(CacheStalenessOptions.class)))
            .thenReturn(false);

        QualifyingService service = new QualifyingService(ergastApiClient, qualifyingRepository, cacheStalenessService, new ObjectMapper());
        List<RaceWithQualifying> races = service.getQualifyingBySeason("2025");

        assertThat(races).hasSize(1);
        assertThat(races.getFirst().getQualifyingResults()).hasSize(1);
        verifyNoInteractions(ergastApiClient);
    }

    @Test
    void shouldFetchAndPersistWhenStale() {
        String payload = """
            {
              "MRData": {
                "RaceTable": {
                  "Races": [
                    {
                      "round": "1",
                      "QualifyingResults": [
                        {
                          "number": "44",
                          "position": "1",
                          "Q1": "1:25.123",
                          "Q2": "1:24.999",
                          "Q3": "1:24.500",
                          "Driver": { "driverId": "hamilton" },
                          "Constructor": { "constructorId": "mercedes" }
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """;

        Qualifying persisted = new Qualifying();
        persisted.setSeason("2025");
        persisted.setRound("1");
        persisted.setDriverId("hamilton");
        persisted.setConstructorId("mercedes");

        when(qualifyingRepository.findBySeason("2025"))
            .thenReturn(List.of())
            .thenReturn(List.of(persisted));
        when(qualifyingRepository.findBySeasonAndRound("2025", "1")).thenReturn(List.of());
        when(cacheStalenessService.shouldFetch(eq("2025"), eq(DataType.QUALIFYING), any(CacheStalenessOptions.class)))
            .thenReturn(true);
        when(ergastApiClient.getJson("/2025/qualifying.json?limit=1000")).thenReturn(payload);

        QualifyingService service = new QualifyingService(ergastApiClient, qualifyingRepository, cacheStalenessService, new ObjectMapper());
        List<RaceWithQualifying> races = service.getQualifyingBySeason("2025");

        assertThat(races).hasSize(1);
        verify(qualifyingRepository, times(1)).saveAll(anyList());
    }

    @Test
    void shouldFallbackToCacheWhenSeasonFetchFails() {
        Qualifying cached = new Qualifying();
        cached.setSeason("2025");
        cached.setRound("2");
        cached.setDriverId("norris");
        cached.setConstructorId("mclaren");

        when(qualifyingRepository.findBySeason("2025")).thenReturn(List.of(cached));
        when(cacheStalenessService.shouldFetch(eq("2025"), eq(DataType.QUALIFYING), any(CacheStalenessOptions.class)))
            .thenReturn(true);
        when(ergastApiClient.getJson("/2025/qualifying.json?limit=1000")).thenThrow(new RuntimeException("boom"));

        QualifyingService service = new QualifyingService(ergastApiClient, qualifyingRepository, cacheStalenessService, new ObjectMapper());
        List<RaceWithQualifying> races = service.getQualifyingBySeason("2025");

        assertThat(races).hasSize(1);
        assertThat(races.getFirst().getRound()).isEqualTo("2");
    }

    @Test
    void shouldFindDriverQualifyingAfterRaceRefresh() {
        Qualifying refreshed = new Qualifying();
        refreshed.setSeason("2025");
        refreshed.setRound("3");
        refreshed.setDriverId("leclerc");
        refreshed.setConstructorId("ferrari");

        when(qualifyingRepository.findBySeasonAndRound("2025", "3"))
            .thenReturn(List.of())
            .thenReturn(List.of(refreshed))
            .thenReturn(List.of(refreshed));
        when(qualifyingRepository.saveAll(anyList())).thenReturn(List.of(refreshed));
        when(ergastApiClient.getJson("/2025/3/qualifying/")).thenReturn("""
            {
              "MRData": {
                "RaceTable": {
                  "Races": [
                    {
                      "round": "3",
                      "QualifyingResults": [
                        {
                          "number": "16",
                          "position": "1",
                          "Q1": "1:24.999",
                          "Driver": { "driverId": "leclerc" },
                          "Constructor": { "constructorId": "ferrari" }
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);

        QualifyingService service = new QualifyingService(ergastApiClient, qualifyingRepository, cacheStalenessService, new ObjectMapper());
        Optional<Qualifying> result = service.getQualifyingByDriver("2025", "3", "leclerc");

        assertThat(result).isPresent();
        assertThat(result.get().getDriverId()).isEqualTo("leclerc");
    }
}

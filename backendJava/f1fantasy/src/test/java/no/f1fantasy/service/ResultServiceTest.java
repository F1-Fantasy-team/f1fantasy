package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Result;
import no.f1fantasy.repository.ResultRepository;
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
class ResultServiceTest {

    @Mock
    private ErgastApiClient ergastApiClient;

    @Mock
    private ResultRepository resultRepository;

    @Mock
    private CacheStalenessService cacheStalenessService;

    @Test
    void shouldUseCacheWhenNotStale() {
        Result cached = new Result();
        cached.setSeason("2025");
        cached.setRound("1");
        cached.setDriverId("hamilton");
        cached.setConstructorId("mercedes");

        when(resultRepository.findBySeason("2025")).thenReturn(List.of(cached));
        when(cacheStalenessService.shouldFetch(eq("2025"), eq(DataType.RESULTS), any(CacheStalenessOptions.class)))
            .thenReturn(false);

        ResultService service = new ResultService(ergastApiClient, resultRepository, cacheStalenessService, new ObjectMapper());
        List<RaceWithResults> races = service.getResultsBySeason("2025");

        assertThat(races).hasSize(1);
        assertThat(races.getFirst().getResults()).hasSize(1);
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
                      "Results": [
                        {
                          "number": "44",
                          "position": "1",
                          "positionText": "1",
                          "points": "25",
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

        Result persisted = new Result();
        persisted.setSeason("2025");
        persisted.setRound("1");
        persisted.setDriverId("hamilton");
        persisted.setConstructorId("mercedes");

        when(resultRepository.findBySeason("2025"))
          .thenReturn(List.of())
          .thenReturn(List.of(persisted));
        when(resultRepository.findBySeasonAndRoundAndIsSprint("2025", "1", false)).thenReturn(List.of());
        when(cacheStalenessService.shouldFetch(eq("2025"), eq(DataType.RESULTS), any(CacheStalenessOptions.class)))
            .thenReturn(true);
        when(ergastApiClient.getJson("/2025/results/?limit=1000")).thenReturn(payload);

        ResultService service = new ResultService(ergastApiClient, resultRepository, cacheStalenessService, new ObjectMapper());
        List<RaceWithResults> races = service.getResultsBySeason("2025");

        assertThat(races).hasSize(1);
        verify(resultRepository, times(1)).saveAll(anyList());
    }

    @Test
    void shouldFallbackToCacheWhenSeasonFetchFails() {
        Result cached = new Result();
        cached.setSeason("2025");
        cached.setRound("2");
        cached.setDriverId("norris");
        cached.setConstructorId("mclaren");

        when(resultRepository.findBySeason("2025")).thenReturn(List.of(cached));
        when(cacheStalenessService.shouldFetch(eq("2025"), eq(DataType.RESULTS), any(CacheStalenessOptions.class)))
            .thenReturn(true);
        when(ergastApiClient.getJson("/2025/results/?limit=1000")).thenThrow(new RuntimeException("boom"));

        ResultService service = new ResultService(ergastApiClient, resultRepository, cacheStalenessService, new ObjectMapper());
        List<RaceWithResults> races = service.getResultsBySeason("2025");

        assertThat(races).hasSize(1);
        assertThat(races.getFirst().getRound()).isEqualTo("2");
    }

    @Test
    void shouldFindResultByDriverAfterRaceRefresh() {
        Result refreshed = new Result();
        refreshed.setSeason("2025");
        refreshed.setRound("3");
        refreshed.setDriverId("leclerc");
        refreshed.setConstructorId("ferrari");

        when(resultRepository.findBySeasonAndRoundAndDriverId("2025", "3", "leclerc"))
            .thenReturn(Optional.empty())
            .thenReturn(Optional.of(refreshed));
        when(resultRepository.findBySeasonAndRoundAndIsSprint("2025", "3", false)).thenReturn(List.of());
        when(resultRepository.saveAll(anyList())).thenReturn(List.of(refreshed));
        when(ergastApiClient.getJson("/2025/3/results/")).thenReturn("""
            {
              "MRData": {
                "RaceTable": {
                  "Races": [
                    {
                      "round": "3",
                      "Results": [
                        {
                          "number": "16",
                          "position": "1",
                          "points": "25",
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

        ResultService service = new ResultService(ergastApiClient, resultRepository, cacheStalenessService, new ObjectMapper());
        Optional<Result> result = service.getResultByDriver("2025", "3", "leclerc");

        assertThat(result).isPresent();
        assertThat(result.get().getDriverId()).isEqualTo("leclerc");
    }

    @Test
    void shouldFetchSprintResultsByRace() {
        Result sprintResult = new Result();
        sprintResult.setSeason("2025");
        sprintResult.setRound("4");
        sprintResult.setDriverId("piastri");
        sprintResult.setConstructorId("mclaren");
        sprintResult.setSprint(true);

        when(resultRepository.findBySeasonAndRoundAndIsSprint("2025", "4", true))
            .thenReturn(List.of())
            .thenReturn(List.of(sprintResult));
        when(ergastApiClient.getJson("/2025/4/sprint/")).thenReturn("""
            {
              "MRData": {
                "RaceTable": {
                  "Races": [
                    {
                      "round": "4",
                      "SprintResults": [
                        {
                          "number": "81",
                          "position": "1",
                          "points": "8",
                          "Driver": { "driverId": "piastri" },
                          "Constructor": { "constructorId": "mclaren" }
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);

        ResultService service = new ResultService(ergastApiClient, resultRepository, cacheStalenessService, new ObjectMapper());
        Optional<RaceWithResults> race = service.getSprintResultsByRace("2025", "4");

        assertThat(race).isPresent();
        assertThat(race.get().getSprintResults()).hasSize(1);
        assertThat(race.get().getSprintResults().getFirst().isSprint()).isTrue();
    }
}

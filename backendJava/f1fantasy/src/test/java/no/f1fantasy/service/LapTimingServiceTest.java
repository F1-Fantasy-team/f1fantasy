package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.LapTiming;
import no.f1fantasy.repository.LapTimingRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.anyList;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@SuppressWarnings("null")
class LapTimingServiceTest {

    @Mock
    private ErgastApiClient ergastApiClient;

    @Mock
    private LapTimingRepository lapTimingRepository;

    @Test
    void shouldFetchAndPersistLapsByRace() {
        LapTiming persisted = new LapTiming();
        persisted.setSeason("2025");
        persisted.setRound("1");
        persisted.setLapNumber("1");
        persisted.setDriverId("hamilton");
        persisted.setPosition("1");

        when(lapTimingRepository.findBySeasonAndRound("2025", "1"))
            .thenReturn(List.of())
            .thenReturn(List.of(persisted));
        when(ergastApiClient.getJson("/2025/1/laps/")).thenReturn("""
            {
              "MRData": {
                "RaceTable": {
                  "Races": [
                    {
                      "Laps": [
                        {
                          "number": "1",
                          "Timings": [
                            {
                              "driverId": "hamilton",
                              "position": "1",
                              "time": "1:32.111"
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);

        LapTimingService service = new LapTimingService(ergastApiClient, lapTimingRepository, new ObjectMapper());
        Optional<RaceWithLaps> race = service.getLapsByRace("2025", "1");

        assertThat(race).isPresent();
        assertThat(race.get().getLaps()).hasSize(1);
        assertThat(race.get().getLaps().getFirst().getTimings()).hasSize(1);
        verify(lapTimingRepository, times(1)).saveAll(anyList());
    }

    @Test
    void shouldFallbackToCacheWhenRaceFetchFails() {
        LapTiming cachedLap2 = new LapTiming();
        cachedLap2.setSeason("2025");
        cachedLap2.setRound("2");
        cachedLap2.setLapNumber("2");
        cachedLap2.setDriverId("norris");
        cachedLap2.setPosition("2");

        LapTiming cachedLap1 = new LapTiming();
        cachedLap1.setSeason("2025");
        cachedLap1.setRound("2");
        cachedLap1.setLapNumber("1");
        cachedLap1.setDriverId("norris");
        cachedLap1.setPosition("1");

        when(lapTimingRepository.findBySeasonAndRound("2025", "2")).thenReturn(List.of(cachedLap2, cachedLap1));
        when(ergastApiClient.getJson("/2025/2/laps/")).thenThrow(new RuntimeException("boom"));

        LapTimingService service = new LapTimingService(ergastApiClient, lapTimingRepository, new ObjectMapper());
        Optional<RaceWithLaps> race = service.getLapsByRace("2025", "2");

        assertThat(race).isPresent();
        assertThat(race.get().getLaps()).hasSize(2);
        assertThat(race.get().getLaps().get(0).getNumber()).isEqualTo("1");
        assertThat(race.get().getLaps().get(1).getNumber()).isEqualTo("2");
    }

    @Test
    void shouldFindLapByNumberAfterRaceRefresh() {
        LapTiming refreshed = new LapTiming();
        refreshed.setSeason("2025");
        refreshed.setRound("3");
        refreshed.setLapNumber("5");
        refreshed.setDriverId("leclerc");
        refreshed.setPosition("1");

        when(lapTimingRepository.findBySeasonAndRound("2025", "3"))
            .thenReturn(List.of())
            .thenReturn(List.of())
            .thenReturn(List.of(refreshed));
        when(ergastApiClient.getJson("/2025/3/laps/")).thenReturn("""
            {
              "MRData": {
                "RaceTable": {
                  "Races": [
                    {
                      "Laps": [
                        {
                          "number": "5",
                          "Timings": [
                            {
                              "driverId": "leclerc",
                              "position": "1",
                              "time": "1:30.111"
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);

        LapTimingService service = new LapTimingService(ergastApiClient, lapTimingRepository, new ObjectMapper());
        Optional<RaceWithLaps.Lap> lap = service.getLapByNumber("2025", "3", "5");

        assertThat(lap).isPresent();
        assertThat(lap.get().getNumber()).isEqualTo("5");
        assertThat(lap.get().getTimings()).hasSize(1);
        assertThat(lap.get().getTimings().getFirst().getDriverId()).isEqualTo("leclerc");
    }

    @Test
    void shouldFindLapsByDriverAfterRaceRefresh() {
        LapTiming refreshed = new LapTiming();
        refreshed.setSeason("2025");
        refreshed.setRound("4");
        refreshed.setLapNumber("7");
        refreshed.setDriverId("piastri");
        refreshed.setPosition("1");

        when(lapTimingRepository.findBySeasonAndRound("2025", "4"))
            .thenReturn(List.of())
            .thenReturn(List.of())
            .thenReturn(List.of(refreshed));
        when(ergastApiClient.getJson("/2025/4/laps/")).thenReturn("""
            {
              "MRData": {
                "RaceTable": {
                  "Races": [
                    {
                      "Laps": [
                        {
                          "number": "7",
                          "Timings": [
                            {
                              "driverId": "piastri",
                              "position": "1",
                              "time": "1:29.999"
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);

        LapTimingService service = new LapTimingService(ergastApiClient, lapTimingRepository, new ObjectMapper());
        List<LapTiming> timings = service.getLapsByDriver("2025", "4", "piastri");

        assertThat(timings).hasSize(1);
        assertThat(timings.getFirst().getDriverId()).isEqualTo("piastri");
        assertThat(timings.getFirst().getLapNumber()).isEqualTo("7");
    }
}

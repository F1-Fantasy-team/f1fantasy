package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.PitStop;
import no.f1fantasy.repository.PitStopRepository;
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
class PitStopServiceTest {

    @Mock
    private ErgastApiClient ergastApiClient;

    @Mock
    private PitStopRepository pitStopRepository;

    @Test
    void shouldFetchAndPersistPitStopsByRace() {
        PitStop persisted = new PitStop();
        persisted.setSeason("2025");
        persisted.setRound("1");
        persisted.setDriverId("hamilton");
        persisted.setLap("12");
        persisted.setStop("1");

        when(pitStopRepository.findBySeasonAndRound("2025", "1"))
            .thenReturn(List.of())
            .thenReturn(List.of(persisted));
        when(ergastApiClient.getJson("/2025/1/pitstops/")).thenReturn("""
            {
              "MRData": {
                "RaceTable": {
                  "Races": [
                    {
                      "PitStops": [
                        {
                          "driverId": "hamilton",
                          "lap": "12",
                          "stop": "1",
                          "time": "14:23:11",
                          "duration": "2.3"
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);

        PitStopService service = new PitStopService(ergastApiClient, pitStopRepository, new ObjectMapper());
        Optional<RaceWithPitStops> race = service.getPitStopsByRace("2025", "1");

        assertThat(race).isPresent();
        assertThat(race.get().getPitStops()).hasSize(1);
        verify(pitStopRepository, times(1)).saveAll(anyList());
    }

    @Test
    void shouldFallbackToCacheWhenRaceFetchFails() {
        PitStop cached = new PitStop();
        cached.setSeason("2025");
        cached.setRound("2");
        cached.setDriverId("norris");
        cached.setLap("10");
        cached.setStop("1");

        when(pitStopRepository.findBySeasonAndRound("2025", "2")).thenReturn(List.of(cached));
        when(ergastApiClient.getJson("/2025/2/pitstops/")).thenThrow(new RuntimeException("boom"));

        PitStopService service = new PitStopService(ergastApiClient, pitStopRepository, new ObjectMapper());
        Optional<RaceWithPitStops> race = service.getPitStopsByRace("2025", "2");

        assertThat(race).isPresent();
        assertThat(race.get().getPitStops()).hasSize(1);
        assertThat(race.get().getPitStops().getFirst().getDriverId()).isEqualTo("norris");
    }

    @Test
    void shouldFindPitStopsByDriverAfterRaceRefresh() {
        PitStop refreshed = new PitStop();
        refreshed.setSeason("2025");
        refreshed.setRound("3");
        refreshed.setDriverId("leclerc");
        refreshed.setLap("18");
        refreshed.setStop("1");

        when(pitStopRepository.findBySeasonAndRound("2025", "3"))
            .thenReturn(List.of())
            .thenReturn(List.of(refreshed))
            .thenReturn(List.of(refreshed));
        when(ergastApiClient.getJson("/2025/3/pitstops/")).thenReturn("""
            {
              "MRData": {
                "RaceTable": {
                  "Races": [
                    {
                      "PitStops": [
                        {
                          "driverId": "leclerc",
                          "lap": "18",
                          "stop": "1",
                          "time": "14:31:12",
                          "duration": "2.5"
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);

        PitStopService service = new PitStopService(ergastApiClient, pitStopRepository, new ObjectMapper());
        List<PitStop> pitStops = service.getPitStopsByDriver("2025", "3", "leclerc");

        assertThat(pitStops).hasSize(1);
        assertThat(pitStops.getFirst().getDriverId()).isEqualTo("leclerc");
    }
}

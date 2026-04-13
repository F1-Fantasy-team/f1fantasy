package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.DriverStanding;
import no.f1fantasy.repository.DriverStandingRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyList;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@SuppressWarnings("null")
class DriverStandingServiceTest {

    @Mock
    private ErgastApiClient ergastApiClient;

    @Mock
    private DriverStandingRepository driverStandingRepository;

    @Mock
    private CacheStalenessService cacheStalenessService;

    @Test
    void shouldFetchAndPersistDriverStandingsBySeason() {
        DriverStanding persisted = new DriverStanding();
        persisted.setSeason("2025");
        persisted.setRound("2");
        persisted.setDriverId("norris");
        persisted.setConstructorId("mclaren");
        persisted.setPosition("1");
        persisted.setPositionText("1");
        persisted.setPoints("44");
        persisted.setWins("1");

        when(cacheStalenessService.shouldFetch(eq("2025"), eq(DataType.DRIVER_STANDINGS), any(CacheStalenessOptions.class))).thenReturn(true);
        when(driverStandingRepository.findLatestBySeason("2025"))
            .thenReturn(List.of())
            .thenReturn(List.of(persisted));
        when(driverStandingRepository.findBySeasonAndRound("2025", "2")).thenReturn(List.of());
        when(ergastApiClient.getJson("/2025/driverstandings/")).thenReturn("""
            {
              "MRData": {
                "StandingsTable": {
                  "StandingsLists": [
                    {
                      "round": "2",
                      "DriverStandings": [
                        {
                          "position": "1",
                          "positionText": "1",
                          "points": "44",
                          "wins": "1",
                          "Driver": { "driverId": "norris" },
                          "Constructors": [ { "constructorId": "mclaren" } ]
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);

        DriverStandingService service = new DriverStandingService(
            ergastApiClient,
            driverStandingRepository,
            cacheStalenessService,
            new ObjectMapper());

        List<DriverStanding> standings = service.getDriverStandingsBySeason("2025");

        assertThat(standings).hasSize(1);
        assertThat(standings.getFirst().getDriverId()).isEqualTo("norris");
        verify(driverStandingRepository, times(1)).saveAll(anyList());
    }

    @Test
    void shouldFallbackToCachedDriverStandingsWhenRoundFetchFails() {
        DriverStanding cached = new DriverStanding();
        cached.setSeason("2025");
        cached.setRound("3");
        cached.setDriverId("piastri");
        cached.setConstructorId("mclaren");
        cached.setPosition("2");
        cached.setPositionText("2");
        cached.setPoints("32");
        cached.setWins("0");

        when(driverStandingRepository.findBySeasonAndRound("2025", "3")).thenReturn(List.of(cached));
        when(ergastApiClient.getJson("/2025/3/driverstandings/")).thenThrow(new RuntimeException("boom"));

        DriverStandingService service = new DriverStandingService(
            ergastApiClient,
            driverStandingRepository,
            cacheStalenessService,
            new ObjectMapper());

        List<DriverStanding> standings = service.getDriverStandingsByRound("2025", "3");

        assertThat(standings).hasSize(1);
        assertThat(standings.getFirst().getDriverId()).isEqualTo("piastri");
    }

    @Test
    void shouldFindDriverStandingAfterRoundRefresh() {
        DriverStanding refreshed = new DriverStanding();
        refreshed.setSeason("2025");
        refreshed.setRound("4");
        refreshed.setDriverId("leclerc");
        refreshed.setConstructorId("ferrari");
        refreshed.setPosition("3");
        refreshed.setPositionText("3");
        refreshed.setPoints("28");
        refreshed.setWins("0");

        when(driverStandingRepository.findBySeasonAndRound("2025", "4"))
            .thenReturn(List.of())
            .thenReturn(List.of())
            .thenReturn(List.of(refreshed));
        when(ergastApiClient.getJson("/2025/4/driverstandings/")).thenReturn("""
            {
              "MRData": {
                "StandingsTable": {
                  "StandingsLists": [
                    {
                      "round": "4",
                      "DriverStandings": [
                        {
                          "position": "3",
                          "positionText": "3",
                          "points": "28",
                          "wins": "0",
                          "Driver": { "driverId": "leclerc" },
                          "Constructors": [ { "constructorId": "ferrari" } ]
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);

        DriverStandingService service = new DriverStandingService(
            ergastApiClient,
            driverStandingRepository,
            cacheStalenessService,
            new ObjectMapper());

        Optional<DriverStanding> standing = service.getDriverStandingByDriver("2025", "4", "leclerc");

        assertThat(standing).isPresent();
        assertThat(standing.get().getDriverId()).isEqualTo("leclerc");
    }
}

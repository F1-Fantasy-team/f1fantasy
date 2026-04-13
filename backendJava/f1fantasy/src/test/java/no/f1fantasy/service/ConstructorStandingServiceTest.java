package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.ConstructorStanding;
import no.f1fantasy.repository.ConstructorStandingRepository;
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
class ConstructorStandingServiceTest {

    @Mock
    private ErgastApiClient ergastApiClient;

    @Mock
    private ConstructorStandingRepository constructorStandingRepository;

    @Mock
    private CacheStalenessService cacheStalenessService;

    @Test
    void shouldFetchAndPersistConstructorStandingsBySeason() {
        ConstructorStanding persisted = new ConstructorStanding();
        persisted.setSeason("2025");
        persisted.setRound("2");
        persisted.setConstructorId("mclaren");
        persisted.setPosition("1");
        persisted.setPositionText("1");
        persisted.setPoints("76");
        persisted.setWins("2");

        when(cacheStalenessService.shouldFetch(eq("2025"), eq(DataType.CONSTRUCTOR_STANDINGS), any(CacheStalenessOptions.class))).thenReturn(true);
        when(constructorStandingRepository.findLatestBySeason("2025"))
            .thenReturn(List.of())
            .thenReturn(List.of(persisted));
        when(constructorStandingRepository.findBySeasonAndRound("2025", "2")).thenReturn(List.of());
        when(ergastApiClient.getJson("/2025/constructorstandings/")).thenReturn("""
            {
              "MRData": {
                "StandingsTable": {
                  "StandingsLists": [
                    {
                      "round": "2",
                      "ConstructorStandings": [
                        {
                          "position": "1",
                          "positionText": "1",
                          "points": "76",
                          "wins": "2",
                          "Constructor": { "constructorId": "mclaren" }
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);

        ConstructorStandingService service = new ConstructorStandingService(
            ergastApiClient,
            constructorStandingRepository,
            cacheStalenessService,
            new ObjectMapper());

        List<ConstructorStanding> standings = service.getConstructorStandingsBySeason("2025");

        assertThat(standings).hasSize(1);
        assertThat(standings.getFirst().getConstructorId()).isEqualTo("mclaren");
        verify(constructorStandingRepository, times(1)).saveAll(anyList());
    }

    @Test
    void shouldFallbackToCachedConstructorStandingsWhenRoundFetchFails() {
        ConstructorStanding cached = new ConstructorStanding();
        cached.setSeason("2025");
        cached.setRound("3");
        cached.setConstructorId("ferrari");
        cached.setPosition("2");
        cached.setPositionText("2");
        cached.setPoints("60");
        cached.setWins("1");

        when(constructorStandingRepository.findBySeasonAndRound("2025", "3")).thenReturn(List.of(cached));
        when(ergastApiClient.getJson("/2025/3/constructorstandings/")).thenThrow(new RuntimeException("boom"));

        ConstructorStandingService service = new ConstructorStandingService(
            ergastApiClient,
            constructorStandingRepository,
            cacheStalenessService,
            new ObjectMapper());

        List<ConstructorStanding> standings = service.getConstructorStandingsByRound("2025", "3");

        assertThat(standings).hasSize(1);
        assertThat(standings.getFirst().getConstructorId()).isEqualTo("ferrari");
    }

    @Test
    void shouldFindConstructorStandingAfterRoundRefresh() {
        ConstructorStanding refreshed = new ConstructorStanding();
        refreshed.setSeason("2025");
        refreshed.setRound("4");
        refreshed.setConstructorId("red_bull");
        refreshed.setPosition("3");
        refreshed.setPositionText("3");
        refreshed.setPoints("41");
        refreshed.setWins("0");

        when(constructorStandingRepository.findBySeasonAndRound("2025", "4"))
            .thenReturn(List.of())
            .thenReturn(List.of())
            .thenReturn(List.of(refreshed));
        when(ergastApiClient.getJson("/2025/4/constructorstandings/")).thenReturn("""
            {
              "MRData": {
                "StandingsTable": {
                  "StandingsLists": [
                    {
                      "round": "4",
                      "ConstructorStandings": [
                        {
                          "position": "3",
                          "positionText": "3",
                          "points": "41",
                          "wins": "0",
                          "Constructor": { "constructorId": "red_bull" }
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);

        ConstructorStandingService service = new ConstructorStandingService(
            ergastApiClient,
            constructorStandingRepository,
            cacheStalenessService,
            new ObjectMapper());

        Optional<ConstructorStanding> standing = service.getConstructorStandingByConstructor("2025", "4", "red_bull");

        assertThat(standing).isPresent();
        assertThat(standing.get().getConstructorId()).isEqualTo("red_bull");
    }
}

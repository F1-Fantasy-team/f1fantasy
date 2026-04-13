package no.f1fantasy.service;

import no.f1fantasy.entity.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;
import java.util.Map;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
@SuppressWarnings("null")
class ScoringServiceTest {

    @Mock
    private PredictionService predictionService;

    @Mock
    private DriverStandingService driverStandingService;

    @Mock
    private ConstructorStandingService constructorStandingService;

    @Mock
    private ResultService resultService;

    @Mock
    private QualifyingService qualifyingService;

    @Mock
    private RaceService raceService;

    @Test
    void shouldCalculateDriverDraftScoreFromDriverStandings() {
        DriverDraftPrediction prediction = new DriverDraftPrediction();
        prediction.setDriver1Id("norris");
        prediction.setDriver2Id("piastri");

        DriverStanding norris = new DriverStanding();
        norris.setDriverId("norris");
        norris.setPoints("110");
        DriverStanding piastri = new DriverStanding();
        piastri.setDriverId("piastri");
        piastri.setPoints("95");

        when(predictionService.getDriverDraft(1, "u1")).thenReturn(Optional.of(prediction));
        when(driverStandingService.getDriverStandingsBySeason("2025")).thenReturn(List.of(norris, piastri));

        ScoringService service = createService();
        int score = service.calculateDriverDraftScore(1, "u1", "2025");

        assertThat(score).isEqualTo(205);
    }

    @Test
    void shouldCalculateWildcardScoreOnlyWhenFulfilled() {
        WildcardPrediction wildcard = new WildcardPrediction();
        wildcard.setFullfilled(true);
        wildcard.setPointsPotential(150);

        when(predictionService.getWildcard(2, "u2")).thenReturn(Optional.of(wildcard));

        ScoringService service = createService();
        int score = service.calculateWildcardScore(2, "u2");

        assertThat(score).isEqualTo(150);
    }

    @Test
    void shouldAggregateAllCategoryScores() {
        when(predictionService.getConstructorChampionship(3, "u3")).thenReturn(Optional.empty());
        when(predictionService.getDriverChampionship(3, "u3")).thenReturn(Optional.empty());
        when(predictionService.getDriverDraft(3, "u3")).thenReturn(Optional.empty());
        when(predictionService.getDestructor(3, "u3")).thenReturn(Optional.empty());
        when(predictionService.getMrSaturday(3, "u3")).thenReturn(Optional.empty());
        when(predictionService.getZeroPointer(3, "u3")).thenReturn(Optional.empty());
        when(predictionService.getWildcard(3, "u3")).thenReturn(Optional.empty());

        ScoringService service = createService();
        Map<String, Integer> scores = service.calculateAllCategoryScores(3, "u3", "2025");

        assertThat(scores).containsKeys(
            "constructorChampionship",
            "driverChampionship",
            "driverDraft",
            "destructor",
            "mrSaturday",
            "zeroPointer",
            "wildcard"
        );
        assertThat(scores.values()).allMatch(v -> v == 0);
    }

    private ScoringService createService() {
        return new ScoringService(
            predictionService,
            driverStandingService,
            constructorStandingService,
            resultService,
            qualifyingService,
            raceService
        );
    }
}

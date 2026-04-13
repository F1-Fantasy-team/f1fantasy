package no.f1fantasy.service;

import no.f1fantasy.entity.*;
import org.springframework.stereotype.Service;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Optional;

@Service
public class ScoringService {

    private static final int CHAMPIONSHIP_EXACT_MATCH_POINTS = 10;
    private static final int CHAMPIONSHIP_POSITION_PENALTY = -2;
    private static final int DESTRUCTOR_DNF_POINTS = 20;
    private static final int MR_SATURDAY_QUALI_WIN_POINTS = 10;
    private static final int ZERO_POINTER_POINTS = 100;
    private static final int ZERO_POINTER_PENALTY = -20;

    private final PredictionService predictionService;
    private final DriverStandingService driverStandingService;
    private final ConstructorStandingService constructorStandingService;
    private final ResultService resultService;
    private final QualifyingService qualifyingService;
    private final RaceService raceService;

    public ScoringService(
        PredictionService predictionService,
        DriverStandingService driverStandingService,
        ConstructorStandingService constructorStandingService,
        ResultService resultService,
        QualifyingService qualifyingService,
        RaceService raceService
    ) {
        this.predictionService = predictionService;
        this.driverStandingService = driverStandingService;
        this.constructorStandingService = constructorStandingService;
        this.resultService = resultService;
        this.qualifyingService = qualifyingService;
        this.raceService = raceService;
    }

    public void ensureSeasonDataAvailable(String season) {
        driverStandingService.getDriverStandingsBySeason(season);
        constructorStandingService.getConstructorStandingsBySeason(season);
        qualifyingService.getQualifyingBySeason(season);
        resultService.getResultsBySeason(season);
    }

    public int calculateConstructorChampionshipScore(Integer groupId, String userId, String season) {
        Optional<ConstructorChampionshipPrediction> predictionOpt = predictionService.getConstructorChampionship(groupId, userId);
        if (predictionOpt.isEmpty()) {
            return 0;
        }

        List<String> actual = constructorStandingService.getConstructorStandingsBySeason(season)
            .stream()
            .sorted((a, b) -> Integer.compare(parseIntOrMax(a.getPosition()), parseIntOrMax(b.getPosition())))
            .map(ConstructorStanding::getConstructorId)
            .toList();

        return calculateChampionshipScore(predictionOpt.get().getRankedConstructorIds(), actual);
    }

    public int calculateDriverChampionshipScore(Integer groupId, String userId, String season) {
        Optional<DriverChampionshipPrediction> predictionOpt = predictionService.getDriverChampionship(groupId, userId);
        if (predictionOpt.isEmpty()) {
            return 0;
        }

        List<String> actual = driverStandingService.getDriverStandingsBySeason(season)
            .stream()
            .sorted((a, b) -> Integer.compare(parseIntOrMax(a.getPosition()), parseIntOrMax(b.getPosition())))
            .map(DriverStanding::getDriverId)
            .toList();

        return calculateChampionshipScore(predictionOpt.get().getRankedDriverIds(), actual);
    }

    public int calculateDriverDraftScore(Integer groupId, String userId, String season) {
        Optional<DriverDraftPrediction> predictionOpt = predictionService.getDriverDraft(groupId, userId);
        if (predictionOpt.isEmpty()) {
            return 0;
        }

        DriverDraftPrediction prediction = predictionOpt.get();
        Map<String, Integer> driverPoints = driverStandingService.getDriverStandingsBySeason(season)
            .stream()
            .collect(java.util.stream.Collectors.toMap(DriverStanding::getDriverId, standing -> parseIntOrDefault(standing.getPoints(), 0), (a, b) -> a));

        int total = 0;
        if (prediction.getDriver1Id() != null) {
            total += driverPoints.getOrDefault(prediction.getDriver1Id(), 0);
        }
        if (prediction.getDriver2Id() != null) {
            total += driverPoints.getOrDefault(prediction.getDriver2Id(), 0);
        }

        return total;
    }

    public int calculateDestructorScore(Integer groupId, String userId, String season) {
        Optional<DestructorPrediction> predictionOpt = predictionService.getDestructor(groupId, userId);
        if (predictionOpt.isEmpty()) {
            return 0;
        }

        DestructorPrediction prediction = predictionOpt.get();
        List<Result> allResults = resultService.getResultsBySeason(season)
            .stream()
            .flatMap(race -> race.getResults() == null ? java.util.stream.Stream.empty() : race.getResults().stream())
            .toList();

        int total = 0;
        if (prediction.getDriver1Id() != null) {
            total += (int) allResults.stream().filter(r -> prediction.getDriver1Id().equals(r.getDriverId()) && isDnf(r.getStatus())).count() * DESTRUCTOR_DNF_POINTS;
        }
        if (prediction.getDriver2Id() != null) {
            total += (int) allResults.stream().filter(r -> prediction.getDriver2Id().equals(r.getDriverId()) && isDnf(r.getStatus())).count() * DESTRUCTOR_DNF_POINTS;
        }

        return total;
    }

    public int calculateMrSaturdayScore(Integer groupId, String userId, String season) {
        Optional<MrSaturdayPrediction> predictionOpt = predictionService.getMrSaturday(groupId, userId);
        if (predictionOpt.isEmpty()) {
            return 0;
        }

        MrSaturdayPrediction prediction = predictionOpt.get();
        List<RaceWithQualifying> races = qualifyingService.getQualifyingBySeason(season);
        int total = 0;

        for (RaceWithQualifying race : races) {
            List<Qualifying> qualifying = race.getQualifyingResults();
            if (qualifying == null || qualifying.isEmpty()) {
                continue;
            }

            total += scoreTeammateBattle(prediction.getDriver1Id(), qualifying);
            total += scoreTeammateBattle(prediction.getDriver2Id(), qualifying);
        }

        return total;
    }

    public int calculateZeroPointerScore(Integer groupId, String userId, String season) {
        Optional<ZeroPointerPrediction> predictionOpt = predictionService.getZeroPointer(groupId, userId);
        if (predictionOpt.isEmpty() || predictionOpt.get().getDriverIds() == null || predictionOpt.get().getDriverIds().isEmpty()) {
            return 0;
        }

        int totalRaces = raceService.getRacesForSeason(season).size();
        Optional<Integer> latestRoundWithResults = resultService.getLatestRoundWithResults(season);
        if (latestRoundWithResults.isEmpty() || latestRoundWithResults.get() < totalRaces) {
            return 0;
        }

        Map<String, Integer> driverPoints = driverStandingService.getDriverStandingsBySeason(season)
            .stream()
            .collect(java.util.stream.Collectors.toMap(DriverStanding::getDriverId, standing -> parseIntOrDefault(standing.getPoints(), 0), (a, b) -> a));

        int total = 0;
        for (String driverId : predictionOpt.get().getDriverIds()) {
            total += driverPoints.getOrDefault(driverId, 0) == 0 ? ZERO_POINTER_POINTS : ZERO_POINTER_PENALTY;
        }

        return total;
    }

    @SuppressWarnings("null")
    public int calculateWildcardScore(Integer groupId, String userId) {
        Optional<WildcardPrediction> predictionOpt = predictionService.getWildcard(groupId, userId);
        if (predictionOpt.isEmpty()) {
            return 0;
        }

        WildcardPrediction prediction = predictionOpt.get();
        if (prediction.getFullfilled() == null || !prediction.getFullfilled()) {
            return 0;
        }

        return Objects.requireNonNullElse(prediction.getPointsPotential(), 0);
    }

    public Map<String, Integer> calculateAllCategoryScores(Integer groupId, String userId, String season) {
        Map<String, Integer> scores = new HashMap<>();
        scores.put("constructorChampionship", calculateConstructorChampionshipScore(groupId, userId, season));
        scores.put("driverChampionship", calculateDriverChampionshipScore(groupId, userId, season));
        scores.put("driverDraft", calculateDriverDraftScore(groupId, userId, season));
        scores.put("destructor", calculateDestructorScore(groupId, userId, season));
        scores.put("mrSaturday", calculateMrSaturdayScore(groupId, userId, season));
        scores.put("zeroPointer", calculateZeroPointerScore(groupId, userId, season));
        scores.put("wildcard", calculateWildcardScore(groupId, userId));
        return scores;
    }

    private int calculateChampionshipScore(List<String> predicted, List<String> actual) {
        if (predicted == null || predicted.isEmpty() || actual == null || actual.isEmpty()) {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < predicted.size(); i++) {
            String predictedId = predicted.get(i);
            int actualIndex = actual.indexOf(predictedId);
            if (actualIndex < 0) {
                continue;
            }

            int delta = Math.abs(i - actualIndex);
            total += CHAMPIONSHIP_EXACT_MATCH_POINTS + (delta * CHAMPIONSHIP_POSITION_PENALTY);
        }

        return total;
    }

    private int scoreTeammateBattle(String predictedDriverId, List<Qualifying> qualifying) {
        if (predictedDriverId == null) {
            return 0;
        }

        Qualifying predicted = qualifying.stream()
            .filter(item -> predictedDriverId.equals(item.getDriverId()))
            .findFirst()
            .orElse(null);

        if (predicted == null || predicted.getConstructorId() == null) {
            return 0;
        }

        Qualifying teammate = qualifying.stream()
            .filter(item -> !predictedDriverId.equals(item.getDriverId()))
            .filter(item -> predicted.getConstructorId().equals(item.getConstructorId()))
            .findFirst()
            .orElse(null);

        if (teammate == null) {
            return MR_SATURDAY_QUALI_WIN_POINTS;
        }

        int predictedPos = parseIntOrMax(predicted.getPosition());
        int teammatePos = parseIntOrMax(teammate.getPosition());
        return predictedPos < teammatePos ? MR_SATURDAY_QUALI_WIN_POINTS : 0;
    }

    private boolean isDnf(String status) {
        if (status == null) {
            return false;
        }

        String normalized = status.toLowerCase();
        return normalized.contains("retired") || normalized.contains("disqualified") || normalized.contains("accident") || normalized.contains("collision") || normalized.contains("engine") || normalized.contains("gearbox") || normalized.contains("hydraul");
    }

    private int parseIntOrMax(String value) {
        if (value == null) {
            return Integer.MAX_VALUE;
        }

        try {
            return Integer.parseInt(value);
        } catch (NumberFormatException ex) {
            return Integer.MAX_VALUE;
        }
    }

    private int parseIntOrDefault(String value, int defaultValue) {
        if (value == null) {
            return defaultValue;
        }

        try {
            return Integer.parseInt(value);
        } catch (NumberFormatException ex) {
            return defaultValue;
        }
    }
}

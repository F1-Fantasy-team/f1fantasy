package no.f1fantasy.controller;

import java.util.List;
import java.util.HashMap;
import java.util.Map;

import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import no.f1fantasy.service.RaceWithResults;
import no.f1fantasy.service.ResultService;

@RestController
@RequestMapping("/api/result")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class ResultController {

    private final ResultService resultService;

    public ResultController(ResultService resultService) {
        this.resultService = resultService;
    }

    @GetMapping("/season/{season}")
    public ResponseEntity<?> getResultsBySeason(@PathVariable String season) {
        try {
            List<RaceWithResults> results = resultService.getResultsBySeason(season);
            return ResponseEntity.ok(results);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching results for season: " + season));
        }
    }

    @GetMapping("/season/{season}/round/{round}")
    public ResponseEntity<?> getResultsByRace(@PathVariable String season, @PathVariable String round) {
        try {
            java.util.Optional<RaceWithResults> results = resultService.getResultsByRace(season, round);
            if (!results.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("No results found for season " + season + ", round " + round));
            }
            return ResponseEntity.ok(results.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching results for season " + season + ", round " + round));
        }
    }

    @GetMapping("/season/{season}/round/{round}/driver/{driverId}")
    public ResponseEntity<?> getResultByDriver(@PathVariable String season, @PathVariable String round, @PathVariable String driverId) {
        try {
            java.util.Optional<no.f1fantasy.entity.Result> result = resultService.getResultByDriver(season, round, driverId);
            if (!result.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("No result found for driver " + driverId + " in season " + season + ", round " + round));
            }
            return ResponseEntity.ok(result.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching result for driver " + driverId));
        }
    }

    @GetMapping("/cached")
    public ResponseEntity<?> getCachedResults() {
        try {
            List<no.f1fantasy.entity.Result> results = resultService.getCachedResults();
            if (results == null || results.isEmpty()) {
                Map<String, Object> response = new HashMap<>();
                response.put("message", "No cached results found");
                response.put("results", results);
                return ResponseEntity.ok(response);
            }
            return ResponseEntity.ok(results);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching cached results"));
        }
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new HashMap<>();
        error.put("error", message);
        return error;
    }
}

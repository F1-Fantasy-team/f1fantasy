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

import no.f1fantasy.entity.LapTiming;
import no.f1fantasy.service.LapTimingService;
import no.f1fantasy.service.RaceWithLaps;

@RestController
@RequestMapping("/api/laptiming")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class LapTimingController {

    private final LapTimingService lapTimingService;

    public LapTimingController(LapTimingService lapTimingService) {
        this.lapTimingService = lapTimingService;
    }

    @GetMapping("/season/{season}/round/{round}")
    public ResponseEntity<?> getLapsByRace(@PathVariable String season, @PathVariable String round) {
        try {
            java.util.Optional<RaceWithLaps> laps = lapTimingService.getLapsByRace(season, round);
            if (!laps.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("No lap timings found for season " + season + ", round " + round));
            }
            return ResponseEntity.ok(laps.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching lap timings for season " + season + ", round " + round));
        }
    }

    @GetMapping("/season/{season}/round/{round}/lap/{lapNumber}")
    public ResponseEntity<?> getLapByNumber(@PathVariable String season, @PathVariable String round, @PathVariable String lapNumber) {
        try {
            java.util.Optional<RaceWithLaps.Lap> lap = lapTimingService.getLapByNumber(season, round, lapNumber);
            if (!lap.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("No timings found for lap " + lapNumber + " in season " + season + ", round " + round));
            }
            return ResponseEntity.ok(lap.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching lap " + lapNumber));
        }
    }

    @GetMapping("/season/{season}/round/{round}/driver/{driverId}")
    public ResponseEntity<?> getLapsByDriver(@PathVariable String season, @PathVariable String round, @PathVariable String driverId) {
        try {
            List<LapTiming> laps = lapTimingService.getLapsByDriver(season, round, driverId);
            if (laps == null || laps.isEmpty()) {
                return ResponseEntity.status(404).body(errorMap("No lap timings found for driver " + driverId + " in season " + season + ", round " + round));
            }
            return ResponseEntity.ok(laps);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching lap timings for driver " + driverId));
        }
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new HashMap<>();
        error.put("error", message);
        return error;
    }
}

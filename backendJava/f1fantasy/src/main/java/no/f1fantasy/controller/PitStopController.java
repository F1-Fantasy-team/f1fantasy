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

import no.f1fantasy.entity.PitStop;
import no.f1fantasy.service.PitStopService;
import no.f1fantasy.service.RaceWithPitStops;

@RestController
@RequestMapping("/api/pitstop")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class PitStopController {

    private final PitStopService pitStopService;

    public PitStopController(PitStopService pitStopService) {
        this.pitStopService = pitStopService;
    }

    @GetMapping("/season/{season}/round/{round}")
    public ResponseEntity<?> getPitStopsByRace(@PathVariable String season, @PathVariable String round) {
        try {
            java.util.Optional<RaceWithPitStops> pitStops = pitStopService.getPitStopsByRace(season, round);
            if (!pitStops.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("No pit stops found for season " + season + ", round " + round));
            }
            return ResponseEntity.ok(pitStops.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching pit stops for season " + season + ", round " + round));
        }
    }

    @GetMapping("/season/{season}/round/{round}/driver/{driverId}")
    public ResponseEntity<?> getPitStopsByDriver(@PathVariable String season, @PathVariable String round, @PathVariable String driverId) {
        try {
            List<PitStop> pitStops = pitStopService.getPitStopsByDriver(season, round, driverId);
            if (pitStops == null || pitStops.isEmpty()) {
                return ResponseEntity.status(404).body(errorMap("No pit stops found for driver " + driverId + " in season " + season + ", round " + round));
            }
            return ResponseEntity.ok(pitStops);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching pit stops for driver " + driverId));
        }
    }

    @GetMapping("/cached")
    public ResponseEntity<?> getCachedPitStops() {
        try {
            List<PitStop> pitStops = pitStopService.getCachedPitStops();
            if (pitStops == null || pitStops.isEmpty()) {
                Map<String, Object> response = new HashMap<>();
                response.put("message", "No cached pit stops found");
                response.put("pitStops", pitStops);
                return ResponseEntity.ok(response);
            }
            return ResponseEntity.ok(pitStops);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching cached pit stops"));
        }
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new HashMap<>();
        error.put("error", message);
        return error;
    }
}

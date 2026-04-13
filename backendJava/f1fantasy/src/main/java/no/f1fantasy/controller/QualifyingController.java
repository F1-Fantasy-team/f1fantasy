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

import no.f1fantasy.entity.Qualifying;
import no.f1fantasy.service.QualifyingService;
import no.f1fantasy.service.RaceWithQualifying;

@RestController
@RequestMapping("/api/qualifying")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class QualifyingController {

    private final QualifyingService qualifyingService;

    public QualifyingController(QualifyingService qualifyingService) {
        this.qualifyingService = qualifyingService;
    }

    @GetMapping("/season/{season}")
    public ResponseEntity<?> getQualifyingBySeason(@PathVariable String season) {
        try {
            List<RaceWithQualifying> qualifying = qualifyingService.getQualifyingBySeason(season);
            return ResponseEntity.ok(qualifying);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching qualifying for season: " + season));
        }
    }

    @GetMapping("/season/{season}/round/{round}")
    public ResponseEntity<?> getQualifyingByRace(@PathVariable String season, @PathVariable String round) {
        try {
            java.util.Optional<RaceWithQualifying> qualifying = qualifyingService.getQualifyingByRace(season, round);
            if (!qualifying.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("No qualifying found for season " + season + ", round " + round));
            }
            return ResponseEntity.ok(qualifying.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching qualifying for season " + season + ", round " + round));
        }
    }

    @GetMapping("/season/{season}/round/{round}/driver/{driverId}")
    public ResponseEntity<?> getQualifyingByDriver(@PathVariable String season, @PathVariable String round, @PathVariable String driverId) {
        try {
            java.util.Optional<Qualifying> qualifying = qualifyingService.getQualifyingByDriver(season, round, driverId);
            if (!qualifying.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("No qualifying found for driver " + driverId + " in season " + season + ", round " + round));
            }
            return ResponseEntity.ok(qualifying.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching qualifying for driver " + driverId));
        }
    }

    @GetMapping("/cached")
    public ResponseEntity<?> getCachedQualifying() {
        try {
            List<Qualifying> qualifying = qualifyingService.getCachedQualifying();
            if (qualifying.isEmpty()) {
                Map<String, Object> response = new HashMap<>();
                response.put("message", "No cached qualifying found");
                response.put("qualifying", qualifying);
                return ResponseEntity.ok(response);
            }
            return ResponseEntity.ok(qualifying);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching cached qualifying"));
        }
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new HashMap<>();
        error.put("error", message);
        return error;
    }
}

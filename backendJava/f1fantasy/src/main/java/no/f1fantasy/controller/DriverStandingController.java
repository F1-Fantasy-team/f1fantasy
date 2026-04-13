package no.f1fantasy.controller;

import java.util.HashMap;
import java.util.Map;

import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import no.f1fantasy.entity.DriverStanding;
import no.f1fantasy.service.DriverStandingService;

@RestController
@RequestMapping("/api/driver-standing")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class DriverStandingController {

    private final DriverStandingService driverStandingService;

    public DriverStandingController(DriverStandingService driverStandingService) {
        this.driverStandingService = driverStandingService;
    }

    @GetMapping("/season/{season}")
    public ResponseEntity<?> getDriverStandingsBySeason(@PathVariable String season) {
        try {
            java.util.List<DriverStanding> standings = driverStandingService.getDriverStandingsBySeason(season);
            if (standings == null || standings.isEmpty()) {
                return ResponseEntity.status(404).body(errorMap("No driver standings found for season " + season));
            }
            return ResponseEntity.ok(standings);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching driver standings for season " + season));
        }
    }

    @GetMapping("/season/{season}/round/{round}")
    public ResponseEntity<?> getDriverStandingsByRound(@PathVariable String season, @PathVariable String round) {
        try {
            java.util.List<DriverStanding> standings = driverStandingService.getDriverStandingsByRound(season, round);
            if (standings == null || standings.isEmpty()) {
                return ResponseEntity.status(404).body(errorMap("No driver standings found for season " + season + ", round " + round));
            }
            return ResponseEntity.ok(standings);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching driver standings for season " + season + ", round " + round));
        }
    }

    @GetMapping("/season/{season}/round/{round}/driver/{driverId}")
    public ResponseEntity<?> getDriverStandingByDriver(@PathVariable String season, @PathVariable String round, @PathVariable String driverId) {
        try {
            java.util.Optional<DriverStanding> standing = driverStandingService.getDriverStandingByDriver(season, round, driverId);
            if (!standing.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("No driver standing found for driver " + driverId + " in season " + season + ", round " + round));
            }
            return ResponseEntity.ok(standing.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching driver standing for driver " + driverId));
        }
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new HashMap<>();
        error.put("error", message);
        return error;
    }
}

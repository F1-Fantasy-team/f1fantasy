package no.f1fantasy.controller;

import java.util.HashMap;
import java.util.Map;

import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import no.f1fantasy.entity.ConstructorStanding;
import no.f1fantasy.service.ConstructorStandingService;

@RestController
@RequestMapping("/api/constructor-standing")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class ConstructorStandingController {

    private final ConstructorStandingService constructorStandingService;

    public ConstructorStandingController(ConstructorStandingService constructorStandingService) {
        this.constructorStandingService = constructorStandingService;
    }

    @GetMapping("/season/{season}")
    public ResponseEntity<?> getConstructorStandingsBySeason(@PathVariable String season) {
        try {
            java.util.List<ConstructorStanding> standings = constructorStandingService.getConstructorStandingsBySeason(season);
            if (standings == null || standings.isEmpty()) {
                return ResponseEntity.status(404).body(errorMap("No constructor standings found for season " + season));
            }
            return ResponseEntity.ok(standings);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching constructor standings for season " + season));
        }
    }

    @GetMapping("/season/{season}/round/{round}")
    public ResponseEntity<?> getConstructorStandingsByRound(@PathVariable String season, @PathVariable String round) {
        try {
            java.util.List<ConstructorStanding> standings = constructorStandingService.getConstructorStandingsByRound(season, round);
            if (standings == null || standings.isEmpty()) {
                return ResponseEntity.status(404).body(errorMap("No constructor standings found for season " + season + ", round " + round));
            }
            return ResponseEntity.ok(standings);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching constructor standings for season " + season + ", round " + round));
        }
    }

    @GetMapping("/season/{season}/round/{round}/constructor/{constructorId}")
    public ResponseEntity<?> getConstructorStandingByConstructor(@PathVariable String season, @PathVariable String round, @PathVariable String constructorId) {
        try {
            java.util.Optional<ConstructorStanding> standing = constructorStandingService.getConstructorStandingByConstructor(season, round, constructorId);
            if (!standing.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("No constructor standing found for constructor " + constructorId + " in season " + season + ", round " + round));
            }
            return ResponseEntity.ok(standing.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching constructor standing for constructor " + constructorId));
        }
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new HashMap<>();
        error.put("error", message);
        return error;
    }
}

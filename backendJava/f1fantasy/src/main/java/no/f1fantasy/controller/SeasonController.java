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

import no.f1fantasy.entity.Season;
import no.f1fantasy.service.SeasonService;

@RestController
@RequestMapping("/api/season")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class SeasonController {

    private final SeasonService seasonService;

    public SeasonController(SeasonService seasonService) {
        this.seasonService = seasonService;
    }

    @GetMapping
    public ResponseEntity<?> getAllSeasons() {
        try {
            List<Season> seasons = seasonService.getAllSeasons();
            return ResponseEntity.ok(seasons);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching seasons"));
        }
    }

    @GetMapping("/{year}")
    public ResponseEntity<?> getSeasonByYear(@PathVariable String year) {
        try {
            java.util.Optional<Season> season = seasonService.getSeasonByYear(year);
            if (!season.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("Season not found: " + year));
            }
            return ResponseEntity.ok(season.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching season: " + year));
        }
    }

    @GetMapping("/cached")
    public ResponseEntity<?> getCachedSeasons() {
        try {
            List<Season> seasons = seasonService.getAllSeasons();
            if (seasons.isEmpty()) {
                Map<String, Object> response = new HashMap<>();
                response.put("message", "No cached seasons found");
                response.put("seasons", seasons);
                return ResponseEntity.ok(response);
            }
            return ResponseEntity.ok(seasons);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching cached seasons"));
        }
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new HashMap<>();
        error.put("error", message);
        return error;
    }
}

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

import no.f1fantasy.entity.Race;
import no.f1fantasy.service.RaceService;

@RestController
@RequestMapping("/api/race")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class RaceController {

    private final RaceService raceService;

    public RaceController(RaceService raceService) {
        this.raceService = raceService;
    }

    @GetMapping
    public ResponseEntity<?> getAllRaces() {
        try {
            List<Race> races = raceService.getAllRaces();
            return ResponseEntity.ok(races);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching races"));
        }
    }

    @GetMapping("/{season}")
    public ResponseEntity<?> getRacesBySeason(@PathVariable String season) {
        try {
            List<Race> races = raceService.getRacesForSeason(season);
            return ResponseEntity.ok(races);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching races for season: " + season));
        }
    }

    @GetMapping("/{season}/{round}")
    public ResponseEntity<?> getRaceByRound(@PathVariable String season, @PathVariable String round) {
        try {
            java.util.Optional<Race> race = raceService.getRaceByRound(season, round);
            if (!race.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("Race not found for season " + season + ", round " + round));
            }
            return ResponseEntity.ok(race.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching race for season " + season + ", round " + round));
        }
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new HashMap<>();
        error.put("error", message);
        return error;
    }
}

package no.f1fantasy.controller;

import java.security.Principal;
import java.util.List;
import java.util.Map;

import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import no.f1fantasy.dto.ErrorResponse;
import no.f1fantasy.dto.SuccessResponse;
import no.f1fantasy.entity.Standing;
import no.f1fantasy.service.ScoringService;
import no.f1fantasy.service.StandingsService;

@RestController
@RequestMapping("/api/standings")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class StandingsController {
    
    private final StandingsService standingsService;
    private final ScoringService scoringService;

    public StandingsController(StandingsService standingsService, ScoringService scoringService) {
        this.standingsService = standingsService;
        this.scoringService = scoringService;
    }

    private String getUserId(Principal principal) {
        if (principal == null) {
            throw new IllegalArgumentException("User ID not found");
        }
        return principal.getName();
    }

    @GetMapping("/groups/{groupId}")
    public ResponseEntity<?> getStandings(
            @PathVariable int groupId,
            @RequestParam(defaultValue = "2026") String season) {
        try {
            List<Standing> standings = standingsService.getStandingsWithAutoRecalc(groupId, season);
            return ResponseEntity.ok(standings);
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @PostMapping("/groups/{groupId}/recalculate")
    public ResponseEntity<?> recalculateStandings(
            @PathVariable int groupId,
            @RequestParam(defaultValue = "2026") String season) {
        try {
            standingsService.recalculateStandings(groupId, season);
            return ResponseEntity.ok(new SuccessResponse("Standings recalculated successfully"));
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/groups/{groupId}/detailed")
    public ResponseEntity<?> getDetailedStandings(
            @PathVariable int groupId,
            @RequestParam(defaultValue = "2026") String season) {
        try {
            List<Standing> standings = standingsService.getStandingsWithAutoRecalc(groupId, season);
            return ResponseEntity.ok(standings);
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/groups/{groupId}/user/{userId}/breakdown")
    public ResponseEntity<?> getUserBreakdown(
            @PathVariable int groupId,
            @PathVariable String userId,
            @RequestParam(defaultValue = "2026") String season) {
        try {
            Map<String, Integer> breakdown = scoringService.calculateAllCategoryScores(groupId, userId, season);
            return ResponseEntity.ok(breakdown);
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/groups/{groupId}/me/breakdown")
    public ResponseEntity<?> getMyBreakdown(
            @PathVariable int groupId,
            @RequestParam(defaultValue = "2026") String season,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            Map<String, Integer> breakdown = scoringService.calculateAllCategoryScores(groupId, userId, season);
            return ResponseEntity.ok(breakdown);
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }
}

package no.f1fantasy.controller;

import java.security.Principal;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import no.f1fantasy.entity.WildcardPrediction;
import no.f1fantasy.repository.GroupRepository;
import no.f1fantasy.repository.WildcardPredictionRepository;
import no.f1fantasy.service.ConstructorService;
import no.f1fantasy.service.DriverService;

@RestController
@RequestMapping("/api/admin")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class AdminController {

    private final WildcardPredictionRepository wildcardPredictionRepository;
    private final GroupRepository groupRepository;
    private final DriverService driverService;
    private final ConstructorService constructorService;

    public AdminController(
            WildcardPredictionRepository wildcardPredictionRepository,
            GroupRepository groupRepository,
            DriverService driverService,
            ConstructorService constructorService) {
        this.wildcardPredictionRepository = wildcardPredictionRepository;
        this.groupRepository = groupRepository;
        this.driverService = driverService;
        this.constructorService = constructorService;
    }

    private String getUserId(Principal principal) {
        if (principal == null) {
            throw new IllegalArgumentException("User ID not found");
        }
        return principal.getName();
    }

    @PutMapping("/groups/{groupId}/wildcard/{userId}/points")
    public ResponseEntity<?> setWildcardPoints(
            @PathVariable int groupId,
            @PathVariable String userId,
            @RequestBody SetPointsRequest request,
            Principal principal) {
        try {
            String adminUserId = getUserId(principal);
            if (!isGroupAdmin(groupId, adminUserId)) {
                return ResponseEntity.status(403).body(errorMap("Only group admin can set wildcard points"));
            }

            if (request.getPointsPotential() < 100 || request.getPointsPotential() > 200) {
                return ResponseEntity.status(400).body(errorMap("Points must be between 100 and 200"));
            }

            WildcardPrediction prediction = wildcardPredictionRepository
                .findByGroupIdAndUserId(groupId, userId)
                .orElse(null);
            if (prediction == null) {
                return ResponseEntity.status(404).body(errorMap("Wildcard prediction not found"));
            }

            prediction.setPointsPotential(request.getPointsPotential());
            return ResponseEntity.ok(wildcardPredictionRepository.save(prediction));
        } catch (Exception ex) {
            return ResponseEntity.status(400).body(errorMap(ex.getMessage()));
        }
    }

    @PutMapping("/groups/{groupId}/wildcard/{userId}/fulfilled")
    public ResponseEntity<?> setWildcardFulfilled(
            @PathVariable int groupId,
            @PathVariable String userId,
            @RequestBody SetFulfilledRequest request,
            Principal principal) {
        try {
            String adminUserId = getUserId(principal);
            if (!isGroupAdmin(groupId, adminUserId)) {
                return ResponseEntity.status(403).body(errorMap("Only group admin can set wildcard fulfilled status"));
            }

            WildcardPrediction prediction = wildcardPredictionRepository
                .findByGroupIdAndUserId(groupId, userId)
                .orElse(null);
            if (prediction == null) {
                return ResponseEntity.status(404).body(errorMap("Wildcard prediction not found"));
            }

            prediction.setFullfilled(request.isFulfilled());
            return ResponseEntity.ok(wildcardPredictionRepository.save(prediction));
        } catch (Exception ex) {
            return ResponseEntity.status(400).body(errorMap(ex.getMessage()));
        }
    }

    @GetMapping("/groups/{groupId}/wildcards")
    public ResponseEntity<?> getAllWildcards(@PathVariable int groupId, Principal principal) {
        try {
            String adminUserId = getUserId(principal);
            if (!isGroupAdmin(groupId, adminUserId)) {
                return ResponseEntity.status(403).body(errorMap("Only group admin can view all wildcards"));
            }
            return ResponseEntity.ok(wildcardPredictionRepository.findByGroupId(groupId));
        } catch (Exception ex) {
            return ResponseEntity.status(400).body(errorMap(ex.getMessage()));
        }
    }

    @PostMapping("/populate-season/{season}")
    public ResponseEntity<?> populateSeason(@PathVariable String season) {
        try {
            List<?> drivers = driverService.getDriversBySeason(season);
            List<?> constructors = constructorService.getConstructorsBySeason(season);

            Map<String, Object> response = new LinkedHashMap<>();
            response.put("message", "Successfully populated season " + season);
            response.put("driversCount", drivers.size());
            response.put("constructorsCount", constructors.size());
            return ResponseEntity.ok(response);
        } catch (Exception ex) {
            return ResponseEntity.status(400).body(errorMap(ex.getMessage()));
        }
    }

    private boolean isGroupAdmin(int groupId, String userId) {
        return groupRepository.existsByIdAndAdminUserId(groupId, userId);
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new LinkedHashMap<>();
        error.put("error", message);
        return error;
    }

    public static class SetPointsRequest {
        private int pointsPotential;

        public int getPointsPotential() {
            return pointsPotential;
        }

        public void setPointsPotential(int pointsPotential) {
            this.pointsPotential = pointsPotential;
        }
    }

    public static class SetFulfilledRequest {
        private boolean fulfilled;

        public boolean isFulfilled() {
            return fulfilled;
        }

        public void setFulfilled(boolean fulfilled) {
            this.fulfilled = fulfilled;
        }
    }
}

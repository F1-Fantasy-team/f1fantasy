package no.f1fantasy.controller;

import java.security.Principal;
import java.util.List;

import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import no.f1fantasy.dto.DriverDraftRequest;
import no.f1fantasy.dto.ErrorResponse;
import no.f1fantasy.dto.TwoDriverRequest;
import no.f1fantasy.entity.ConstructorChampionshipPrediction;
import no.f1fantasy.entity.DestructorPrediction;
import no.f1fantasy.entity.DriverChampionshipPrediction;
import no.f1fantasy.entity.DriverDraftPrediction;
import no.f1fantasy.entity.MrSaturdayPrediction;
import no.f1fantasy.entity.WildcardPrediction;
import no.f1fantasy.entity.ZeroPointerPrediction;
import no.f1fantasy.service.PredictionService;

@RestController
@RequestMapping("/api/predictions")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class PredictionsController {
    
    private final PredictionService predictionService;

    public PredictionsController(PredictionService predictionService) {
        this.predictionService = predictionService;
    }

    private String getUserId(Principal principal) {
        if (principal == null) {
            throw new IllegalArgumentException("User ID not found");
        }
        return principal.getName();
    }

    // Constructor Championship
    @PostMapping("/groups/{groupId}/constructor-championship")
    public ResponseEntity<?> saveConstructorChampionship(
            @PathVariable int groupId,
            @RequestBody List<String> rankedConstructorIds,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            ConstructorChampionshipPrediction prediction = predictionService.saveConstructorChampionship(groupId, userId, rankedConstructorIds);
            return ResponseEntity.ok(prediction);
        } catch (IllegalArgumentException ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/groups/{groupId}/constructor-championship")
    public ResponseEntity<?> getConstructorChampionship(
            @PathVariable int groupId,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            var prediction = predictionService.getConstructorChampionship(groupId, userId);
            if (prediction.isEmpty()) {
                return ResponseEntity.notFound().build();
            }
            return ResponseEntity.ok(prediction.get());
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    // Driver Championship
    @PostMapping("/groups/{groupId}/driver-championship")
    public ResponseEntity<?> saveDriverChampionship(
            @PathVariable int groupId,
            @RequestBody List<String> rankedDriverIds,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            DriverChampionshipPrediction prediction = predictionService.saveDriverChampionship(groupId, userId, rankedDriverIds);
            return ResponseEntity.ok(prediction);
        } catch (IllegalArgumentException ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/groups/{groupId}/driver-championship")
    public ResponseEntity<?> getDriverChampionship(
            @PathVariable int groupId,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            var prediction = predictionService.getDriverChampionship(groupId, userId);
            if (prediction.isEmpty()) {
                return ResponseEntity.notFound().build();
            }
            return ResponseEntity.ok(prediction.get());
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    // Driver Draft
    @PostMapping("/groups/{groupId}/driver-draft")
    public ResponseEntity<?> saveDriverDraft(
            @PathVariable int groupId,
            @RequestBody DriverDraftRequest request,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            DriverDraftPrediction prediction = predictionService.saveDriverDraft(groupId, userId, request.getDriver1Id(), request.getDriver2Id());
            return ResponseEntity.ok(prediction);
        } catch (IllegalArgumentException ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/groups/{groupId}/driver-draft")
    public ResponseEntity<?> getDriverDraft(
            @PathVariable int groupId,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            var prediction = predictionService.getDriverDraft(groupId, userId);
            if (prediction.isEmpty()) {
                return ResponseEntity.notFound().build();
            }
            return ResponseEntity.ok(prediction.get());
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    // Destructor
    @PostMapping("/groups/{groupId}/destructor")
    public ResponseEntity<?> saveDestructor(
            @PathVariable int groupId,
            @RequestBody TwoDriverRequest request,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            DestructorPrediction prediction = predictionService.saveDestructor(groupId, userId, request.getDriver1Id(), request.getDriver2Id());
            return ResponseEntity.ok(prediction);
        } catch (IllegalArgumentException ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/groups/{groupId}/destructor")
    public ResponseEntity<?> getDestructor(
            @PathVariable int groupId,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            var prediction = predictionService.getDestructor(groupId, userId);
            if (prediction.isEmpty()) {
                return ResponseEntity.notFound().build();
            }
            return ResponseEntity.ok(prediction.get());
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    // Mr Saturday
    @PostMapping("/groups/{groupId}/mr-saturday")
    public ResponseEntity<?> saveMrSaturday(
            @PathVariable int groupId,
            @RequestBody TwoDriverRequest request,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            MrSaturdayPrediction prediction = predictionService.saveMrSaturday(groupId, userId, request.getDriver1Id(), request.getDriver2Id());
            return ResponseEntity.ok(prediction);
        } catch (IllegalArgumentException ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/groups/{groupId}/mr-saturday")
    public ResponseEntity<?> getMrSaturday(
            @PathVariable int groupId,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            var prediction = predictionService.getMrSaturday(groupId, userId);
            if (prediction.isEmpty()) {
                return ResponseEntity.notFound().build();
            }
            return ResponseEntity.ok(prediction.get());
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    // Zero Pointer
    @PostMapping("/groups/{groupId}/zero-pointer")
    public ResponseEntity<?> saveZeroPointer(
            @PathVariable int groupId,
            @RequestBody List<String> driverIds,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            ZeroPointerPrediction prediction = predictionService.saveZeroPointer(groupId, userId, driverIds);
            return ResponseEntity.ok(prediction);
        } catch (IllegalArgumentException ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/groups/{groupId}/zero-pointer")
    public ResponseEntity<?> getZeroPointer(
            @PathVariable int groupId,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            var prediction = predictionService.getZeroPointer(groupId, userId);
            if (prediction.isEmpty()) {
                return ResponseEntity.notFound().build();
            }
            return ResponseEntity.ok(prediction.get());
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    // Wildcard
    @PostMapping("/groups/{groupId}/wildcard")
    public ResponseEntity<?> saveWildcard(
            @PathVariable int groupId,
            @RequestBody String statement,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            WildcardPrediction prediction = predictionService.saveWildcard(groupId, userId, statement);
            return ResponseEntity.ok(prediction);
        } catch (IllegalArgumentException ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/groups/{groupId}/wildcard")
    public ResponseEntity<?> getWildcard(
            @PathVariable int groupId,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            var prediction = predictionService.getWildcard(groupId, userId);
            if (prediction.isEmpty()) {
                return ResponseEntity.notFound().build();
            }
            return ResponseEntity.ok(prediction.get());
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/groups/{groupId}/wildcards")
    public ResponseEntity<?> getAllWildcards(
            @PathVariable int groupId,
            Principal principal) {
        try {
            String userId = getUserId(principal);
            List<WildcardPrediction> predictions = predictionService.getAllWildcards(groupId, userId);
            return ResponseEntity.ok(predictions);
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }
}

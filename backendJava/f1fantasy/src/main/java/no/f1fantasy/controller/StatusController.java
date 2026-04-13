package no.f1fantasy.controller;

import java.util.List;
import java.util.HashMap;
import java.util.Map;

import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import no.f1fantasy.entity.Status;
import no.f1fantasy.service.StatusService;

@RestController
@RequestMapping("/api/status")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class StatusController {

    private final StatusService statusService;

    public StatusController(StatusService statusService) {
        this.statusService = statusService;
    }

    @GetMapping
    public ResponseEntity<?> getAllStatuses() {
        try {
            List<Status> statuses = statusService.getAllStatuses();
            return ResponseEntity.ok(statuses);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching statuses"));
        }
    }

    @PostMapping("/refresh")
    public ResponseEntity<?> refreshStatuses() {
        try {
            List<Status> statuses = statusService.refreshStatuses();
            return ResponseEntity.ok(statuses);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error refreshing statuses"));
        }
    }

    @GetMapping("/id/{statusId}")
    public ResponseEntity<?> getStatusById(@PathVariable String statusId) {
        try {
            java.util.Optional<Status> status = statusService.getById(statusId);
            if (!status.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("Status not found: " + statusId));
            }
            return ResponseEntity.ok(status.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching status: " + statusId));
        }
    }

    @GetMapping("/text/{statusText}")
    public ResponseEntity<?> getStatusByText(@PathVariable String statusText) {
        try {
            java.util.Optional<Status> status = statusService.getByText(statusText);
            if (!status.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("Status not found: " + statusText));
            }
            return ResponseEntity.ok(status.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching status: " + statusText));
        }
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new HashMap<>();
        error.put("error", message);
        return error;
    }
}

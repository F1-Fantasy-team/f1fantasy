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

import no.f1fantasy.entity.Circuit;
import no.f1fantasy.service.CircuitService;

@RestController
@RequestMapping("/api/circuit")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class CircuitController {

    private final CircuitService circuitService;

    public CircuitController(CircuitService circuitService) {
        this.circuitService = circuitService;
    }

    @GetMapping
    public ResponseEntity<?> getAllCircuits() {
        try {
            List<Circuit> circuits = circuitService.getAllCircuits();
            return ResponseEntity.ok(circuits);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching circuits"));
        }
    }

    @GetMapping("/{circuitId}")
    public ResponseEntity<?> getCircuitById(@PathVariable String circuitId) {
        try {
            java.util.Optional<Circuit> circuit = circuitService.getCircuitById(circuitId);
            if (!circuit.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("Circuit not found: " + circuitId));
            }
            return ResponseEntity.ok(circuit.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching circuit: " + circuitId));
        }
    }

    @GetMapping("/cached")
    public ResponseEntity<?> getCachedCircuits() {
        try {
            List<Circuit> circuits = circuitService.getAllCircuits();
            if (circuits.isEmpty()) {
                Map<String, Object> response = new HashMap<>();
                response.put("message", "No cached circuits found");
                response.put("circuits", circuits);
                return ResponseEntity.ok(response);
            }
            return ResponseEntity.ok(circuits);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching cached circuits"));
        }
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new HashMap<>();
        error.put("error", message);
        return error;
    }
}

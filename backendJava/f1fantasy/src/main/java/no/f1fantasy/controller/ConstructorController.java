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

import no.f1fantasy.entity.Constructor;
import no.f1fantasy.service.ConstructorService;

@RestController
@RequestMapping("/api/constructor")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class ConstructorController {

    private final ConstructorService constructorService;

    public ConstructorController(ConstructorService constructorService) {
        this.constructorService = constructorService;
    }

    @GetMapping
    public ResponseEntity<?> getAllConstructors() {
        try {
            List<Constructor> constructors = constructorService.getAllConstructors();
            return ResponseEntity.ok(constructors);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching constructors"));
        }
    }

    @GetMapping("/season/{season}")
    public ResponseEntity<?> getConstructorsBySeason(@PathVariable String season) {
        try {
            List<Constructor> constructors = constructorService.getConstructorsBySeason(season);
            return ResponseEntity.ok(constructors);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching constructors for season: " + season));
        }
    }

    @GetMapping("/{constructorId}")
    public ResponseEntity<?> getConstructorById(@PathVariable String constructorId) {
        try {
            java.util.Optional<Constructor> constructor = constructorService.getConstructorById(constructorId);
            if (!constructor.isPresent()) {
                return ResponseEntity.status(404).body(errorMap("Constructor not found: " + constructorId));
            }
            return ResponseEntity.ok(constructor.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching constructor: " + constructorId));
        }
    }

    @GetMapping("/cached")
    public ResponseEntity<?> getCachedConstructors() {
        try {
            List<Constructor> constructors = constructorService.getAllConstructors();
            if (constructors.isEmpty()) {
                Map<String, Object> response = new HashMap<>();
                response.put("message", "No cached constructors found");
                response.put("constructors", constructors);
                return ResponseEntity.ok(response);
            }
            return ResponseEntity.ok(constructors);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching cached constructors"));
        }
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new HashMap<>();
        error.put("error", message);
        return error;
    }
}

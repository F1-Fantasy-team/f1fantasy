package no.f1fantasy.controller;

import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.Map;

import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/health")
@SuppressWarnings("null")
public class HealthController {

    @GetMapping
    public ResponseEntity<?> getHealth() {
        Map<String, Object> response = new HashMap<>();
        response.put("status", "healthy");
        response.put("timestamp", OffsetDateTime.now());
        response.put("service", "F1Fantasy API");
        return ResponseEntity.ok(response);
    }

    @GetMapping("/ping")
    public ResponseEntity<String> ping() {
        return ResponseEntity.ok("pong");
    }

    @GetMapping("/ready")
    public ResponseEntity<?> getReadiness() {
        Map<String, Object> response = new HashMap<>();
        response.put("status", "ready");
        response.put("database", "connected");
        response.put("timestamp", OffsetDateTime.now());
        response.put("service", "F1Fantasy API");
        return ResponseEntity.ok(response);
    }
}

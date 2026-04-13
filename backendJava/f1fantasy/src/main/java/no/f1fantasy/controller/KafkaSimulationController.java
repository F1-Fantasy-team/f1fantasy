package no.f1fantasy.controller;

import no.f1fantasy.kafka.F1DataUpdateEvent;
import no.f1fantasy.kafka.F1DataUpdateEventHandler;
import org.springframework.context.annotation.Profile;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.LinkedHashMap;
import java.util.Map;

@RestController
@Profile("dev")
@RequestMapping("/api/admin/kafka")
@PreAuthorize("isAuthenticated()")
public class KafkaSimulationController {

    private final F1DataUpdateEventHandler eventHandler;

    public KafkaSimulationController(F1DataUpdateEventHandler eventHandler) {
        this.eventHandler = eventHandler;
    }

    @PostMapping("/simulate")
    public ResponseEntity<?> simulate(@RequestBody F1DataUpdateEvent event) {
        if (event == null || event.getDataType() == null || event.getDataType().isBlank()) {
            return ResponseEntity.badRequest().body(error("dataType is required"));
        }

        eventHandler.handle(event);

        Map<String, Object> response = new LinkedHashMap<>();
        response.put("message", "Simulated event handled");
        response.put("dataType", event.getDataType());
        response.put("season", event.getSeason());
        response.put("round", event.getRound());
        response.put("updatedAt", event.getUpdatedAt());
        response.put("source", event.getSource());
        return ResponseEntity.ok(response);
    }

    private Map<String, String> error(String message) {
        Map<String, String> map = new LinkedHashMap<>();
        map.put("error", message);
        return map;
    }
}

package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Circuit;
import no.f1fantasy.entity.Location;
import no.f1fantasy.repository.CircuitRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

@Service
public class CircuitService {

    private static final Logger logger = LoggerFactory.getLogger(CircuitService.class);
    private static final String PAGINATION_KEY = "circuits";

    private final ErgastApiClient ergastApiClient;
    private final CircuitRepository circuitRepository;
    private final PaginationStateTracker paginationStateTracker;
    private final ObjectMapper objectMapper;

    public CircuitService(
        ErgastApiClient ergastApiClient,
        CircuitRepository circuitRepository,
        PaginationStateTracker paginationStateTracker,
        ObjectMapper objectMapper
    ) {
        this.ergastApiClient = ergastApiClient;
        this.circuitRepository = circuitRepository;
        this.paginationStateTracker = paginationStateTracker;
        this.objectMapper = objectMapper;
    }

    public List<Circuit> getAllCircuits() {
        List<Circuit> cached = circuitRepository.findAll();
        if (!paginationStateTracker.shouldFetch(PAGINATION_KEY)) {
            return cached;
        }

        int limit = 30;
        int offset = paginationStateTracker.getNextOffset(PAGINATION_KEY, limit);
        int total = 0;

        try {
            do {
                String payload = ergastApiClient.getJson("/circuits/?offset=" + offset);
                JsonNode mrData = objectMapper.readTree(payload).path("MRData");
                total = mrData.path("total").asInt(0);
                JsonNode circuits = mrData.path("CircuitTable").path("Circuits");

                if (!circuits.isArray() || circuits.isEmpty()) {
                    break;
                }

                List<Circuit> batch = new ArrayList<>();
                for (JsonNode circuitNode : circuits) {
                    Circuit circuit = new Circuit();
                    circuit.setCircuitId(circuitNode.path("circuitId").asText());
                    circuit.setUrl(circuitNode.path("url").asText(null));
                    circuit.setCircuitName(circuitNode.path("circuitName").asText(null));

                    JsonNode locationNode = circuitNode.path("Location");
                    if (locationNode.isObject()) {
                        Location location = new Location();
                        location.setLat(locationNode.path("lat").asText(null));
                        location.setLong_(locationNode.path("long").asText(null));
                        location.setLocality(locationNode.path("locality").asText(null));
                        location.setCountry(locationNode.path("country").asText(null));
                        circuit.setLocation(location);
                    }

                    batch.add(circuit);
                }

                circuitRepository.saveAll(batch);
                paginationStateTracker.updateState(PAGINATION_KEY, offset, total, limit);
                offset += limit;
            } while (offset < total);

            if (total == 0 || offset >= total) {
                paginationStateTracker.markComplete(PAGINATION_KEY);
            }

            return circuitRepository.findAll();
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch circuits from Ergast, returning cached data", ex);
            return cached;
        }
    }

    public Optional<Circuit> getCircuitById(String circuitId) {
        Optional<Circuit> circuit = circuitRepository.findByCircuitId(circuitId);
        if (circuit.isPresent()) {
            return circuit;
        }

        getAllCircuits();
        return circuitRepository.findByCircuitId(circuitId);
    }

    public List<Circuit> getCachedCircuits() {
        return circuitRepository.findAll();
    }
}

package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Status;
import no.f1fantasy.repository.StatusRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.Optional;

@Service
public class StatusService {

    private static final Logger logger = LoggerFactory.getLogger(StatusService.class);

    private final ErgastApiClient ergastApiClient;
    private final StatusRepository statusRepository;
    private final ObjectMapper objectMapper;

    public StatusService(
        ErgastApiClient ergastApiClient,
        StatusRepository statusRepository,
        ObjectMapper objectMapper
    ) {
        this.ergastApiClient = ergastApiClient;
        this.statusRepository = statusRepository;
        this.objectMapper = objectMapper;
    }

    public List<Status> getAllStatuses() {
        List<Status> cached = statusRepository.findAll();
        if (!cached.isEmpty()) {
            return cached;
        }

        return fetchAndCacheStatuses();
    }

    public List<Status> refreshStatuses() {
        return fetchAndCacheStatuses();
    }

    public Optional<Status> getById(String statusId) {
        String safeStatusId = Objects.requireNonNull(statusId, "statusId must not be null");
        Optional<Status> cached = statusRepository.findById(safeStatusId);
        if (cached.isPresent()) {
            return cached;
        }

        fetchAndCacheStatuses();
        return statusRepository.findById(safeStatusId);
    }

    public Optional<Status> getByText(String statusText) {
        Optional<Status> cached = statusRepository.findByStatusText(statusText);
        if (cached.isPresent()) {
            return cached;
        }

        fetchAndCacheStatuses();
        return statusRepository.findByStatusText(statusText);
    }

    private List<Status> fetchAndCacheStatuses() {
        List<Status> cached = statusRepository.findAll();

        try {
            String payload = ergastApiClient.getJson("/status?limit=1000");
            JsonNode statusesNode = objectMapper.readTree(payload)
                .path("MRData")
                .path("StatusTable")
                .path("Status");

            if (!statusesNode.isArray()) {
                return cached;
            }

            List<Status> parsed = parseStatuses(statusesNode);
            if (parsed.isEmpty()) {
                return cached;
            }

            statusRepository.saveAll(parsed);
            return statusRepository.findAll();
        } catch (JsonProcessingException | RuntimeException ex) {
            logger.warn("Failed to fetch statuses, returning cached data", ex);
            return cached;
        }
    }

    private List<Status> parseStatuses(JsonNode statusesNode) {
        List<Status> parsed = new ArrayList<>();

        for (JsonNode node : statusesNode) {
            Status status = new Status();
            status.setStatusId(node.path("statusId").asText(null));
            status.setStatusText(node.path("status").asText(null));
            status.setCount(node.path("count").asText(null));

            if (status.getStatusId() != null
                && status.getStatusText() != null
                && status.getCount() != null) {
                parsed.add(status);
            }
        }

        return parsed;
    }
}

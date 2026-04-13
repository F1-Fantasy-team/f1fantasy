package no.f1fantasy.kafka.event;

import java.time.OffsetDateTime;

public record PredictionEvent(
    String eventType,
    String predictionType,
    Integer groupId,
    String userId,
    OffsetDateTime occurredAt
) {
}
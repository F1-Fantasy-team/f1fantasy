package no.f1fantasy.kafka.event;

import java.time.OffsetDateTime;

public record GroupEvent(
    String eventType,
    Integer groupId,
    String actorUserId,
    String targetUserId,
    String groupName,
    String lockMode,
    Boolean predictionsLocked,
    OffsetDateTime occurredAt
) {
}
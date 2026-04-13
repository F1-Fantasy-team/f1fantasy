package no.f1fantasy.kafka;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.stereotype.Component;

@Component
@ConditionalOnProperty(prefix = "f1.kafka.consumer", name = "enabled", havingValue = "true")
public class F1DataUpdateConsumer {

    private static final Logger logger = LoggerFactory.getLogger(F1DataUpdateConsumer.class);

    private final F1DataUpdateEventHandler eventHandler;

    public F1DataUpdateConsumer(F1DataUpdateEventHandler eventHandler) {
        this.eventHandler = eventHandler;
    }

    @KafkaListener(
        topics = "${f1.kafka.topic:f1-data-updates}",
        groupId = "${f1.kafka.consumer.group-id:f1fantasy-consumer}",
        autoStartup = "${f1.kafka.consumer.enabled:false}"
    )
    public void onEvent(F1DataUpdateEvent event) {
        logger.debug("Received Kafka update event type={}, season={}, round={}",
            event == null ? null : event.getDataType(),
            event == null ? null : event.getSeason(),
            event == null ? null : event.getRound());

        eventHandler.handle(event);
    }
}

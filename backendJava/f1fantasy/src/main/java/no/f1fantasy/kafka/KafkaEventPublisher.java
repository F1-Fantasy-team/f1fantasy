package no.f1fantasy.kafka;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.ObjectProvider;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.stereotype.Component;

@Component
public class KafkaEventPublisher implements EventPublisher {

    private static final Logger logger = LoggerFactory.getLogger(KafkaEventPublisher.class);

    private final KafkaTemplate<Object, Object> kafkaTemplate;
    private final String bootstrapServers;

    public KafkaEventPublisher(
        ObjectProvider<KafkaTemplate<Object, Object>> kafkaTemplateProvider,
        @Value("${spring.kafka.bootstrap-servers:none}") String bootstrapServers
    ) {
        this.kafkaTemplate = kafkaTemplateProvider.getIfAvailable();
        this.bootstrapServers = bootstrapServers;
    }

    @Override
    public void publish(String topic, String key, Object event) {
        if (topic == null || topic.isBlank()) {
            return;
        }

        if (bootstrapServers == null || bootstrapServers.isBlank() || "none".equalsIgnoreCase(bootstrapServers.trim())) {
            logger.debug("Kafka publish skipped because bootstrap servers are not configured");
            return;
        }

        if (kafkaTemplate == null) {
            logger.warn("KafkaTemplate is unavailable, event was not published to topic {}", topic);
            return;
        }

        String safeKey = key == null ? "" : key;
        kafkaTemplate.send(topic, safeKey, event).whenComplete((result, throwable) -> {
            if (throwable != null) {
                logger.warn("Failed to publish Kafka event to topic {} with key {}", topic, key, throwable);
                return;
            }

            if (result != null && result.getRecordMetadata() != null) {
                logger.debug(
                    "Published Kafka event to topic {} partition {} offset {}",
                    result.getRecordMetadata().topic(),
                    result.getRecordMetadata().partition(),
                    result.getRecordMetadata().offset()
                );
            }
        });
    }
}
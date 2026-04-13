package no.f1fantasy.kafka;

public interface EventPublisher {

    void publish(String topic, String key, Object event);
}
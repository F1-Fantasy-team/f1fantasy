package no.f1fantasy.controller;

import no.f1fantasy.kafka.F1DataUpdateEvent;
import no.f1fantasy.kafka.F1DataUpdateEventHandler;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.ResponseEntity;

import java.util.Map;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
class KafkaSimulationControllerTest {

    @Mock
    private F1DataUpdateEventHandler eventHandler;

    @Test
    void shouldReturnBadRequestWhenDataTypeMissing() {
        KafkaSimulationController controller = new KafkaSimulationController(eventHandler);
        F1DataUpdateEvent event = new F1DataUpdateEvent();

        ResponseEntity<?> response = controller.simulate(event);

        assertThat(response.getStatusCode().value()).isEqualTo(400);
        assertThat(response.getBody()).isInstanceOf(Map.class);
        verify(eventHandler, never()).handle(event);
    }

    @Test
    void shouldForwardEventToHandler() {
        KafkaSimulationController controller = new KafkaSimulationController(eventHandler);
        F1DataUpdateEvent event = new F1DataUpdateEvent();
        event.setDataType("RESULTS");
        event.setSeason("2026");
        event.setRound("2");

        ResponseEntity<?> response = controller.simulate(event);

        assertThat(response.getStatusCode().value()).isEqualTo(200);
        verify(eventHandler).handle(event);
    }
}

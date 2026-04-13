package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Circuit;
import no.f1fantasy.repository.CircuitRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.OffsetDateTime;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class CircuitServiceTest {

    @Mock
    private ErgastApiClient ergastApiClient;

    @Mock
    private CircuitRepository circuitRepository;

    @Test
    void shouldUseCachedCircuitsWhenStateIsFresh() {
        PaginationStateTracker paginationStateTracker = new PaginationStateTracker();
        paginationStateTracker.getState("circuits").setComplete(true);
        paginationStateTracker.getState("circuits").setLastUpdate(OffsetDateTime.now());

        Circuit cached = new Circuit();
        cached.setCircuitId("bahrain");

        when(circuitRepository.findAll()).thenReturn(List.of(cached));

        CircuitService service = new CircuitService(ergastApiClient, circuitRepository, paginationStateTracker, new ObjectMapper());
        List<Circuit> circuits = service.getAllCircuits();

        assertThat(circuits).hasSize(1);
        verifyNoInteractions(ergastApiClient);
    }

    @Test
    @SuppressWarnings("null")
    void shouldFetchAndPersistWhenStale() {
        String payload = """
            {
              "MRData": {
                "total": "1",
                "CircuitTable": {
                  "Circuits": [
                    {
                      "circuitId": "bahrain",
                      "url": "u",
                      "circuitName": "Bahrain International Circuit",
                      "Location": {
                        "lat": "26.0325",
                        "long": "50.5106",
                        "locality": "Sakhir",
                        "country": "Bahrain"
                      }
                    }
                  ]
                }
              }
            }
            """;

        Circuit persisted = new Circuit();
        persisted.setCircuitId("bahrain");

        when(circuitRepository.findAll())
            .thenReturn(List.of())
            .thenReturn(List.of(persisted));
        when(ergastApiClient.getJson("/circuits/?offset=0")).thenReturn(payload);

        CircuitService service = new CircuitService(ergastApiClient, circuitRepository, new PaginationStateTracker(), new ObjectMapper());
        List<Circuit> circuits = service.getAllCircuits();

        assertThat(circuits).hasSize(1);
        verify(circuitRepository, times(1)).saveAll(anyList());
    }

    @Test
    void shouldFallbackToCacheWhenApiFails() {
        Circuit cached = new Circuit();
        cached.setCircuitId("monza");

        when(circuitRepository.findAll()).thenReturn(List.of(cached));
        when(ergastApiClient.getJson("/circuits/?offset=0")).thenThrow(new RuntimeException("boom"));

        CircuitService service = new CircuitService(ergastApiClient, circuitRepository, new PaginationStateTracker(), new ObjectMapper());
        List<Circuit> circuits = service.getAllCircuits();

        assertThat(circuits).hasSize(1);
        assertThat(circuits.get(0).getCircuitId()).isEqualTo("monza");
    }
}

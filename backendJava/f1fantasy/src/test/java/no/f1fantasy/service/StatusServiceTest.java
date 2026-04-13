package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Status;
import no.f1fantasy.repository.StatusRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.anyList;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@SuppressWarnings("null")
class StatusServiceTest {

    @Mock
    private ErgastApiClient ergastApiClient;

    @Mock
    private StatusRepository statusRepository;

    @Test
    void shouldReturnCachedStatusesWhenAvailable() {
        Status cached = new Status();
        cached.setStatusId("1");
        cached.setStatusText("Finished");
        cached.setCount("1116");

        when(statusRepository.findAll()).thenReturn(List.of(cached));

        StatusService service = new StatusService(ergastApiClient, statusRepository, new ObjectMapper());
        List<Status> statuses = service.getAllStatuses();

        assertThat(statuses).hasSize(1);
        assertThat(statuses.getFirst().getStatusText()).isEqualTo("Finished");
        verifyNoInteractions(ergastApiClient);
    }

    @Test
    void shouldFetchAndPersistStatusesWhenCacheEmpty() {
        Status persisted = new Status();
        persisted.setStatusId("1");
        persisted.setStatusText("Finished");
        persisted.setCount("1116");

        when(statusRepository.findAll())
            .thenReturn(List.of())
            .thenReturn(List.of(persisted));
        when(ergastApiClient.getJson("/status?limit=1000")).thenReturn("""
            {
              "MRData": {
                "StatusTable": {
                  "Status": [
                    {
                      "statusId": "1",
                      "count": "1116",
                      "status": "Finished"
                    }
                  ]
                }
              }
            }
            """);

        StatusService service = new StatusService(ergastApiClient, statusRepository, new ObjectMapper());
        List<Status> statuses = service.getAllStatuses();

        assertThat(statuses).hasSize(1);
        assertThat(statuses.getFirst().getStatusId()).isEqualTo("1");
        verify(statusRepository, times(1)).saveAll(anyList());
    }

    @Test
    void shouldFindStatusByIdAfterRefresh() {
        Status refreshed = new Status();
        refreshed.setStatusId("3");
        refreshed.setStatusText("Accident");
        refreshed.setCount("100");

        when(statusRepository.findById("3"))
            .thenReturn(Optional.empty())
            .thenReturn(Optional.of(refreshed));
        when(statusRepository.findAll())
            .thenReturn(List.of())
            .thenReturn(List.of(refreshed));
        when(ergastApiClient.getJson("/status?limit=1000")).thenReturn("""
            {
              "MRData": {
                "StatusTable": {
                  "Status": [
                    {
                      "statusId": "3",
                      "count": "100",
                      "status": "Accident"
                    }
                  ]
                }
              }
            }
            """);

        StatusService service = new StatusService(ergastApiClient, statusRepository, new ObjectMapper());
        Optional<Status> status = service.getById("3");

        assertThat(status).isPresent();
        assertThat(status.get().getStatusText()).isEqualTo("Accident");
    }

    @Test
    void shouldFallbackToCachedStatusesWhenFetchFails() {
        Status cached = new Status();
        cached.setStatusId("4");
        cached.setStatusText("Collision");
        cached.setCount("80");

        when(statusRepository.findAll()).thenReturn(List.of(cached));
        when(ergastApiClient.getJson("/status?limit=1000")).thenThrow(new RuntimeException("boom"));

        StatusService service = new StatusService(ergastApiClient, statusRepository, new ObjectMapper());
        List<Status> statuses = service.refreshStatuses();

        assertThat(statuses).hasSize(1);
        assertThat(statuses.getFirst().getStatusText()).isEqualTo("Collision");
    }
}

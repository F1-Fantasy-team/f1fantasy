package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Constructor;
import no.f1fantasy.repository.ConstructorRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@SuppressWarnings("null")
class ConstructorServiceTest {

    @Mock
    private ErgastApiClient ergastApiClient;

    @Mock
    private ConstructorRepository constructorRepository;

    @Test
    void shouldUseCachedConstructorsWhenStateIsFresh() {
        PaginationStateTracker paginationStateTracker = new PaginationStateTracker();
        paginationStateTracker.getState("constructors").setComplete(true);
        paginationStateTracker.getState("constructors").setLastUpdate(OffsetDateTime.now());

        Constructor cached = new Constructor();
        cached.setConstructorId("ferrari");

        when(constructorRepository.findAll()).thenReturn(List.of(cached));

        ConstructorService service = new ConstructorService(ergastApiClient, constructorRepository, paginationStateTracker, new ObjectMapper());
        List<Constructor> constructors = service.getAllConstructors();

        assertThat(constructors).hasSize(1);
        verifyNoInteractions(ergastApiClient);
    }

    @Test
    void shouldFetchAndPersistWhenStale() {
        String payload = """
            {
              "MRData": {
                "total": "1",
                "ConstructorTable": {
                  "Constructors": [
                    {
                      "constructorId": "ferrari",
                      "url": "u",
                      "name": "Ferrari",
                      "nationality": "Italian"
                    }
                  ]
                }
              }
            }
            """;

        Constructor persisted = new Constructor();
        persisted.setConstructorId("ferrari");

        when(constructorRepository.findAll())
          .thenReturn(List.of())
          .thenReturn(List.of(persisted));
        when(ergastApiClient.getJson("/constructors/?offset=0")).thenReturn(payload);

        ConstructorService service = new ConstructorService(ergastApiClient, constructorRepository, new PaginationStateTracker(), new ObjectMapper());
        List<Constructor> constructors = service.getAllConstructors();

        assertThat(constructors).hasSize(1);
        verify(constructorRepository, times(1)).saveAll(anyList());
    }

    @Test
    void shouldMergeActiveSeasonsWhenFetchingBySeason() {
        String payload = """
            {
              "MRData": {
                "ConstructorTable": {
                  "Constructors": [
                    {
                      "constructorId": "ferrari",
                      "name": "Ferrari",
                      "nationality": "Italian"
                    }
                  ]
                }
              }
            }
            """;

        Constructor existing = new Constructor();
        existing.setConstructorId("ferrari");
        existing.setActiveSeasons(new ArrayList<>(List.of("2024")));

        when(constructorRepository.findAll()).thenReturn(List.of(existing));
        when(constructorRepository.findAllById(List.of("ferrari"))).thenReturn(List.of(existing));
        when(ergastApiClient.getJson("/2025/constructors/")).thenReturn(payload);

        ConstructorService service = new ConstructorService(ergastApiClient, constructorRepository, new PaginationStateTracker(), new ObjectMapper());
        List<Constructor> constructors = service.getConstructorsBySeason("2025");

        assertThat(constructors).hasSize(1);
        assertThat(constructors.get(0).getActiveSeasons()).containsExactly("2024", "2025");
        verify(constructorRepository, times(1)).saveAll(anyList());
    }

    @Test
    void shouldFindConstructorByIdAfterRefresh() {
        Constructor expected = new Constructor();
        expected.setConstructorId("mclaren");

        when(constructorRepository.findByConstructorId("mclaren"))
          .thenReturn(Optional.empty())
          .thenReturn(Optional.of(expected));
        when(constructorRepository.findAll())
          .thenReturn(List.of())
          .thenReturn(List.of(expected));
        when(ergastApiClient.getJson("/constructors/?offset=0")).thenReturn("""
            {
              "MRData": {
                "total": "1",
                "ConstructorTable": {
                  "Constructors": [
                    {
                      "constructorId": "mclaren",
                      "name": "McLaren",
                      "nationality": "British"
                    }
                  ]
                }
              }
            }
            """);

        ConstructorService service = new ConstructorService(ergastApiClient, constructorRepository, new PaginationStateTracker(), new ObjectMapper());
        Optional<Constructor> found = service.getConstructorById("mclaren");

        assertThat(found).isPresent();
        assertThat(found.get().getConstructorId()).isEqualTo("mclaren");
    }

    @Test
    void shouldFallbackToCachedConstructorsWhenSeasonFetchFails() {
        Constructor cached = new Constructor();
        cached.setConstructorId("williams");

        when(constructorRepository.findAll()).thenReturn(List.of(cached));
        when(ergastApiClient.getJson("/2025/constructors/")).thenThrow(new RuntimeException("boom"));

        ConstructorService service = new ConstructorService(ergastApiClient, constructorRepository, new PaginationStateTracker(), new ObjectMapper());
        List<Constructor> constructors = service.getConstructorsBySeason("2025");

        assertThat(constructors).hasSize(1);
        assertThat(constructors.getFirst().getConstructorId()).isEqualTo("williams");
    }
}

package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.client.ErgastApiClient;
import no.f1fantasy.entity.Driver;
import no.f1fantasy.repository.DriverRepository;
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
class DriverServiceTest {

    @Mock
    private ErgastApiClient ergastApiClient;

    @Mock
    private DriverRepository driverRepository;

    @Test
    void shouldUseCachedDriversWhenStateIsFresh() {
        PaginationStateTracker paginationStateTracker = new PaginationStateTracker();
        paginationStateTracker.getState("drivers").setComplete(true);
        paginationStateTracker.getState("drivers").setLastUpdate(OffsetDateTime.now());

        Driver cached = new Driver();
        cached.setDriverId("max_verstappen");

        when(driverRepository.findAll()).thenReturn(List.of(cached));

        DriverService service = new DriverService(ergastApiClient, driverRepository, paginationStateTracker, new ObjectMapper());
        List<Driver> drivers = service.getAllDrivers();

        assertThat(drivers).hasSize(1);
        verifyNoInteractions(ergastApiClient);
    }

    @Test
    @SuppressWarnings("null")
    void shouldFetchAndPersistWhenStale() {
        String payload = """
            {
              "MRData": {
                "total": "1",
                "DriverTable": {
                  "Drivers": [
                    {
                      "driverId": "max_verstappen",
                      "permanentNumber": "1",
                      "code": "VER",
                      "url": "u",
                      "givenName": "Max",
                      "familyName": "Verstappen",
                      "dateOfBirth": "1997-09-30",
                      "nationality": "Dutch"
                    }
                  ]
                }
              }
            }
            """;

        Driver persisted = new Driver();
        persisted.setDriverId("max_verstappen");

        when(driverRepository.findAll())
            .thenReturn(List.of())
            .thenReturn(List.of(persisted));
        when(ergastApiClient.getJson("/drivers/?offset=0")).thenReturn(payload);

        DriverService service = new DriverService(ergastApiClient, driverRepository, new PaginationStateTracker(), new ObjectMapper());
        List<Driver> drivers = service.getAllDrivers();

        assertThat(drivers).hasSize(1);
        verify(driverRepository, times(1)).saveAll(anyList());
    }

    @Test
    @SuppressWarnings("null")
    void shouldMergeActiveSeasonsWhenFetchingBySeason() {
        String payload = """
            {
              "MRData": {
                "DriverTable": {
                  "Drivers": [
                    {
                      "driverId": "max_verstappen",
                      "givenName": "Max",
                      "familyName": "Verstappen"
                    }
                  ]
                }
              }
            }
            """;

        Driver existing = new Driver();
        existing.setDriverId("max_verstappen");
        existing.setActiveSeasons(new ArrayList<>(List.of("2024")));

        when(driverRepository.findAll()).thenReturn(List.of(existing));
        when(driverRepository.findAllById(List.of("max_verstappen"))).thenReturn(List.of(existing));
        when(ergastApiClient.getJson("/2025/drivers/")).thenReturn(payload);

        DriverService service = new DriverService(ergastApiClient, driverRepository, new PaginationStateTracker(), new ObjectMapper());
        List<Driver> drivers = service.getDriversBySeason("2025");

        assertThat(drivers).hasSize(1);
        assertThat(drivers.get(0).getActiveSeasons()).containsExactly("2024", "2025");
        verify(driverRepository, times(1)).saveAll(List.of(drivers.get(0)));
    }

    @Test
    @SuppressWarnings("null")
    void shouldFindDriverByIdAfterRefresh() {
        Driver expected = new Driver();
        expected.setDriverId("lewis_hamilton");

        when(driverRepository.findByDriverId("lewis_hamilton"))
            .thenReturn(Optional.empty())
            .thenReturn(Optional.of(expected));
        when(driverRepository.findAll())
            .thenReturn(List.of())
            .thenReturn(List.of(expected));
        when(ergastApiClient.getJson("/drivers/?offset=0")).thenReturn("""
            {
              "MRData": {
                "total": "1",
                "DriverTable": {
                  "Drivers": [
                    {
                      "driverId": "lewis_hamilton",
                      "givenName": "Lewis",
                      "familyName": "Hamilton"
                    }
                  ]
                }
              }
            }
            """);

        DriverService service = new DriverService(ergastApiClient, driverRepository, new PaginationStateTracker(), new ObjectMapper());
        Optional<Driver> found = service.getDriverById("lewis_hamilton");

        assertThat(found).isPresent();
        assertThat(found.get().getDriverId()).isEqualTo("lewis_hamilton");
    }

    @Test
    @SuppressWarnings("null")
    void shouldFetchForActiveDriversWhenCacheIsEmptyForSeason() {
        Driver fetched = new Driver();
        fetched.setDriverId("lando_norris");
        fetched.setActiveSeasons(new ArrayList<>(List.of("2025")));

        when(driverRepository.findAll())
            .thenReturn(List.of())
            .thenReturn(List.of())
            .thenReturn(List.of(fetched));
        when(ergastApiClient.getJson("/2025/drivers/")).thenReturn("""
            {
              "MRData": {
                "DriverTable": {
                  "Drivers": [
                    {
                      "driverId": "lando_norris",
                      "givenName": "Lando",
                      "familyName": "Norris"
                    }
                  ]
                }
              }
            }
            """);
        when(driverRepository.findAllById(List.of("lando_norris"))).thenReturn(List.of());

        DriverService service = new DriverService(ergastApiClient, driverRepository, new PaginationStateTracker(), new ObjectMapper());
        List<Driver> active = service.getActiveDrivers("2025");

        assertThat(active).hasSize(1);
        assertThat(active.get(0).getDriverId()).isEqualTo("lando_norris");
    }

      @Test
      @SuppressWarnings("null")
      void shouldFallbackToCachedDriversWhenSeasonFetchFails() {
        Driver cached = new Driver();
        cached.setDriverId("alex_albon");

        when(driverRepository.findAll()).thenReturn(List.of(cached));
        when(ergastApiClient.getJson("/2025/drivers/")).thenThrow(new RuntimeException("boom"));

        DriverService service = new DriverService(ergastApiClient, driverRepository, new PaginationStateTracker(), new ObjectMapper());
        List<Driver> drivers = service.getDriversBySeason("2025");

        assertThat(drivers).hasSize(1);
        assertThat(drivers.getFirst().getDriverId()).isEqualTo("alex_albon");
      }
}

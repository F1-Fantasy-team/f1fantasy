package no.f1fantasy.client;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.config.ErgastProperties;
import no.f1fantasy.entity.Constructor;
import no.f1fantasy.entity.Driver;
import no.f1fantasy.repository.ConstructorRepository;
import no.f1fantasy.repository.DriverRepository;
import no.f1fantasy.service.ConstructorService;
import no.f1fantasy.service.DriverService;
import no.f1fantasy.service.PaginationStateTracker;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.springframework.web.client.RestTemplate;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assumptions.assumeTrue;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

@Tag("external")
@SuppressWarnings("null")
class ErgastValueChainIntegrationTest {

    private static final String ENABLE_PROPERTY = "ergast.integration.enabled";
    private static final String BASE_URL = "https://api.jolpi.ca/ergast/f1/";

    @Test
    void clientShouldFetchRealPayloadFromErgast() throws Exception {
        ErgastApiClient client = createClient();
        assumeExternalApiAvailable(client);

        String payload = client.getJson("/2024/1/results/");
        JsonNode races = new ObjectMapper()
            .readTree(payload)
            .path("MRData")
            .path("RaceTable")
            .path("Races");

        assertThat(races.isArray()).isTrue();
        assertThat(races).isNotEmpty();
        assertThat(races.get(0).path("Results").isArray()).isTrue();
    }

    @Test
    void driverServiceShouldFetchAndParseDriversForSeason() {
        ErgastApiClient client = createClient();
        assumeExternalApiAvailable(client);

        DriverRepository driverRepository = mock(DriverRepository.class);
        when(driverRepository.findAll()).thenReturn(List.of());
        when(driverRepository.findAllById(any())).thenReturn(List.of());

        DriverService driverService = new DriverService(
            client,
            driverRepository,
            new PaginationStateTracker(),
            new ObjectMapper()
        );

        List<Driver> drivers = driverService.getDriversBySeason("2024");

        assertThat(drivers).isNotEmpty();
        assertThat(drivers)
            .extracting(Driver::getDriverId)
            .contains("max_verstappen", "norris");

        Driver max = drivers.stream()
            .filter(driver -> "max_verstappen".equals(driver.getDriverId()))
            .findFirst()
            .orElseThrow();

        assertThat(max.getGivenName()).isEqualTo("Max");
        assertThat(max.getFamilyName()).isEqualTo("Verstappen");
        assertThat(max.getActiveSeasons()).contains("2024");
        assertThat(max.getNationality()).isNotBlank();
    }

    @Test
    void constructorServiceShouldFetchAndParseConstructorsForSeason() {
        ErgastApiClient client = createClient();
        assumeExternalApiAvailable(client);

        ConstructorRepository constructorRepository = mock(ConstructorRepository.class);
        when(constructorRepository.findAll()).thenReturn(List.of());
        when(constructorRepository.findAllById(any())).thenReturn(List.of());

        ConstructorService constructorService = new ConstructorService(
            client,
            constructorRepository,
            new PaginationStateTracker(),
            new ObjectMapper()
        );

        List<Constructor> constructors = constructorService.getConstructorsBySeason("2024");

        assertThat(constructors).isNotEmpty();
        assertThat(constructors)
            .extracting(Constructor::getConstructorId)
            .contains("red_bull", "ferrari", "mercedes");

        Constructor redBull = constructors.stream()
            .filter(constructor -> "red_bull".equals(constructor.getConstructorId()))
            .findFirst()
            .orElseThrow();

        assertThat(redBull.getName()).isNotBlank();
        assertThat(redBull.getActiveSeasons()).contains("2024");
        assertThat(redBull.getNationality()).isNotBlank();
    }

    private ErgastApiClient createClient() {
        ErgastProperties properties = new ErgastProperties();
        properties.setBaseUrl(BASE_URL);
        properties.setPoliteDelayMs(0);
        properties.getRetry().setMaxAttempts(2);
        properties.getRetry().setInitialIntervalMs(200);
        properties.getRetry().setMultiplier(2.0);

        return new ErgastApiClient(new RestTemplate(), properties);
    }

    private void assumeExternalApiAvailable(ErgastApiClient client) {
        assumeTrue(Boolean.getBoolean(ENABLE_PROPERTY),
            () -> "External Ergast integration tests are disabled. Run with -D" + ENABLE_PROPERTY + "=true");

        try {
            String payload = client.getJson("/2024/1/results/");
            assumeTrue(payload != null && !payload.isBlank(), "Ergast API unreachable in current environment");
        } catch (RuntimeException ex) {
            assumeTrue(false, "Ergast API unreachable in current environment: " + ex.getMessage());
        }
    }
}
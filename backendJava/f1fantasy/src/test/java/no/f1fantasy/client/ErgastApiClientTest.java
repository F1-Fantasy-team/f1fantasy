package no.f1fantasy.client;

import no.f1fantasy.config.ErgastProperties;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.http.HttpMethod;
import org.springframework.http.MediaType;
import org.springframework.test.web.client.MockRestServiceServer;
import org.springframework.web.client.RestTemplate;

import java.util.Objects;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.springframework.test.web.client.ExpectedCount.times;
import static org.springframework.test.web.client.match.MockRestRequestMatchers.method;
import static org.springframework.test.web.client.match.MockRestRequestMatchers.requestTo;
import static org.springframework.test.web.client.response.MockRestResponseCreators.withStatus;
import static org.springframework.test.web.client.response.MockRestResponseCreators.withSuccess;

class ErgastApiClientTest {

    private RestTemplate restTemplate;
    private MockRestServiceServer server;
    private ErgastProperties properties;

    @SuppressWarnings("unused")
    @BeforeEach
    void setUp() {
        restTemplate = new RestTemplate();
        server = MockRestServiceServer.bindTo(restTemplate).build();

        properties = new ErgastProperties();
        properties.setBaseUrl("https://ergast.test");
        properties.setPoliteDelayMs(0);
        properties.getRetry().setMaxAttempts(3);
        properties.getRetry().setInitialIntervalMs(1);
        properties.getRetry().setMultiplier(1.0);
    }

    @Test
    void shouldReturnBodyOnSuccessfulCall() {
        server.expect(times(1), requestTo("https://ergast.test/current.json"))
            .andExpect(method(Objects.requireNonNull(HttpMethod.GET)))
            .andRespond(withSuccess("{\"ok\":true}", MediaType.APPLICATION_JSON));

        ErgastApiClient client = new ErgastApiClient(restTemplate, properties);
        String response = client.getJson("/current.json");

        assertThat(response).contains("ok");
        server.verify();
    }

    @Test
    void shouldRetryOn429AndSucceed() {
        server.expect(times(1), requestTo("https://ergast.test/current.json"))
            .andExpect(method(Objects.requireNonNull(HttpMethod.GET)))
            .andRespond(withStatus(org.springframework.http.HttpStatus.TOO_MANY_REQUESTS));
        server.expect(times(1), requestTo("https://ergast.test/current.json"))
            .andExpect(method(Objects.requireNonNull(HttpMethod.GET)))
            .andRespond(withSuccess("{\"retried\":true}", MediaType.APPLICATION_JSON));

        ErgastApiClient client = new ErgastApiClient(restTemplate, properties);
        String response = client.getJson("/current.json");

        assertThat(response).contains("retried");
        server.verify();
    }

    @Test
    void shouldThrowAfterMaxRetries() {
        server.expect(times(3), requestTo("https://ergast.test/current.json"))
            .andExpect(method(Objects.requireNonNull(HttpMethod.GET)))
            .andRespond(withStatus(org.springframework.http.HttpStatus.TOO_MANY_REQUESTS));

        ErgastApiClient client = new ErgastApiClient(restTemplate, properties);

        assertThatThrownBy(() -> client.getJson("/current.json"))
            .isInstanceOf(Exception.class);
        server.verify();
    }
}

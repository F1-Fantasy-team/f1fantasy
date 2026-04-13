package no.f1fantasy.client;

import no.f1fantasy.config.ErgastProperties;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatusCode;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestClientResponseException;
import org.springframework.web.client.RestTemplate;
import org.springframework.web.util.UriComponentsBuilder;

import java.net.URI;
import java.time.Duration;
import java.time.Instant;
import java.util.Objects;
import java.util.concurrent.Semaphore;

@Component
public class ErgastApiClient {

    private static final Logger logger = LoggerFactory.getLogger(ErgastApiClient.class);

    private static final Semaphore RATE_LIMITER = new Semaphore(1);
    private static Instant lastRequestAt = Instant.EPOCH;

    private final RestTemplate restTemplate;
    private final ErgastProperties properties;

    public ErgastApiClient(RestTemplate restTemplate, ErgastProperties properties) {
        this.restTemplate = restTemplate;
        this.properties = properties;
    }

    public String getJson(String path) {
        String baseUrl = Objects.requireNonNull(properties.getBaseUrl(), "ergast.base-url must be configured");
        String requestPath = Objects.requireNonNull(path, "path must not be null");

        URI uri = UriComponentsBuilder.fromUriString(baseUrl)
            .path(requestPath)
            .build(true)
            .toUri();

        int attempts = 0;
        int maxAttempts = properties.getRetry().getMaxAttempts();

        while (attempts < maxAttempts) {
            attempts++;
            enforcePoliteDelay();
            try {
                return restTemplate.getForObject(uri, String.class);
            } catch (RestClientResponseException ex) {
                if (!isTooManyRequests(ex.getStatusCode()) || attempts >= maxAttempts) {
                    throw ex;
                }

                long backoffMs = calculateBackoffMs(attempts - 1);
                logger.warn("Ergast returned 429. Retrying in {}ms (attempt {}/{})", backoffMs, attempts, maxAttempts);
                sleep(backoffMs);
            }
        }

        throw new IllegalStateException("Max retries exceeded while calling Ergast");
    }

    private boolean isTooManyRequests(HttpStatusCode statusCode) {
        return statusCode.value() == 429;
    }

    private long calculateBackoffMs(int retryIndex) {
        double multiplierPow = Math.pow(properties.getRetry().getMultiplier(), retryIndex);
        return (long) (properties.getRetry().getInitialIntervalMs() * multiplierPow);
    }

    private void enforcePoliteDelay() {
        try {
            RATE_LIMITER.acquire();
            long delayMs = properties.getPoliteDelayMs() - Duration.between(lastRequestAt, Instant.now()).toMillis();
            if (delayMs > 0) {
                sleep(delayMs);
            }
            lastRequestAt = Instant.now();
        } catch (InterruptedException interruptedException) {
            Thread.currentThread().interrupt();
            throw new IllegalStateException("Interrupted while waiting for Ergast polite delay", interruptedException);
        } finally {
            RATE_LIMITER.release();
        }
    }

    private void sleep(long delayMs) {
        try {
            Thread.sleep(delayMs);
        } catch (InterruptedException interruptedException) {
            Thread.currentThread().interrupt();
            throw new IllegalStateException("Interrupted while waiting for retry delay", interruptedException);
        }
    }
}

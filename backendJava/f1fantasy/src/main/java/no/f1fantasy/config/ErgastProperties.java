package no.f1fantasy.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "ergast")
public class ErgastProperties {

    private String baseUrl;
    private int pageSize = 30;
    private long politeDelayMs = 100;
    private final Retry retry = new Retry();

    public String getBaseUrl() {
        return baseUrl;
    }

    public void setBaseUrl(String baseUrl) {
        this.baseUrl = baseUrl;
    }

    public int getPageSize() {
        return pageSize;
    }

    public void setPageSize(int pageSize) {
        this.pageSize = pageSize;
    }

    public long getPoliteDelayMs() {
        return politeDelayMs;
    }

    public void setPoliteDelayMs(long politeDelayMs) {
        this.politeDelayMs = politeDelayMs;
    }

    public Retry getRetry() {
        return retry;
    }

    public static class Retry {
        private int maxAttempts = 5;
        private long initialIntervalMs = 500;
        private double multiplier = 2.0;

        public int getMaxAttempts() {
            return maxAttempts;
        }

        public void setMaxAttempts(int maxAttempts) {
            this.maxAttempts = maxAttempts;
        }

        public long getInitialIntervalMs() {
            return initialIntervalMs;
        }

        public void setInitialIntervalMs(long initialIntervalMs) {
            this.initialIntervalMs = initialIntervalMs;
        }

        public double getMultiplier() {
            return multiplier;
        }

        public void setMultiplier(double multiplier) {
            this.multiplier = multiplier;
        }
    }
}

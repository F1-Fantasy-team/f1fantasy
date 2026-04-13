package no.f1fantasy.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "rate-limit")
public class RateLimitProperties {

    private Policy read = new Policy();
    private Policy write = new Policy();
    private Policy admin = new Policy();
    private int violationThreshold = 10;
    private int violationWindowMinutes = 5;

    public Policy getRead() {
        return read;
    }

    public void setRead(Policy read) {
        this.read = read;
    }

    public Policy getWrite() {
        return write;
    }

    public void setWrite(Policy write) {
        this.write = write;
    }

    public Policy getAdmin() {
        return admin;
    }

    public void setAdmin(Policy admin) {
        this.admin = admin;
    }

    public int getViolationThreshold() {
        return violationThreshold;
    }

    public void setViolationThreshold(int violationThreshold) {
        this.violationThreshold = violationThreshold;
    }

    public int getViolationWindowMinutes() {
        return violationWindowMinutes;
    }

    public void setViolationWindowMinutes(int violationWindowMinutes) {
        this.violationWindowMinutes = violationWindowMinutes;
    }

    public static class Policy {
        private long capacity = 60;
        private long refillTokens = 60;
        private long refillPeriodSeconds = 60;

        public long getCapacity() {
            return capacity;
        }

        public void setCapacity(long capacity) {
            this.capacity = capacity;
        }

        public long getRefillTokens() {
            return refillTokens;
        }

        public void setRefillTokens(long refillTokens) {
            this.refillTokens = refillTokens;
        }

        public long getRefillPeriodSeconds() {
            return refillPeriodSeconds;
        }

        public void setRefillPeriodSeconds(long refillPeriodSeconds) {
            this.refillPeriodSeconds = refillPeriodSeconds;
        }
    }
}

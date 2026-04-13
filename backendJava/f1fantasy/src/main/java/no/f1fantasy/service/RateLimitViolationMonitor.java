package no.f1fantasy.service;

import no.f1fantasy.config.RateLimitProperties;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;

@Service
public class RateLimitViolationMonitor {

    private static final Logger logger = LoggerFactory.getLogger(RateLimitViolationMonitor.class);

    private static class ViolationTracker {
        private final List<OffsetDateTime> violations = new ArrayList<>();
    }

    private final ConcurrentHashMap<String, ViolationTracker> trackers = new ConcurrentHashMap<>();
    private final IpBlacklistService blacklistService;
    private final RateLimitProperties properties;

    public RateLimitViolationMonitor(IpBlacklistService blacklistService, RateLimitProperties properties) {
        this.blacklistService = blacklistService;
        this.properties = properties;
    }

    public void recordViolation(String ipAddress) {
        if (ipAddress == null || ipAddress.isBlank()) {
            return;
        }

        ViolationTracker tracker = trackers.computeIfAbsent(ipAddress, key -> new ViolationTracker());
        synchronized (tracker) {
            OffsetDateTime now = OffsetDateTime.now();
            OffsetDateTime threshold = now.minusMinutes(properties.getViolationWindowMinutes());
            tracker.violations.removeIf(time -> time.isBefore(threshold));
            tracker.violations.add(now);

            if (tracker.violations.size() >= properties.getViolationThreshold()) {
                logger.warn("Auto-blacklisting IP {} after {} rate limit violations in {} minutes",
                    ipAddress, tracker.violations.size(), properties.getViolationWindowMinutes());
                blacklistService.blacklist(
                    ipAddress,
                    "Auto-blacklisted for repeated rate-limit violations",
                    Duration.ofHours(1)
                );
                tracker.violations.clear();
            }
        }
    }
}

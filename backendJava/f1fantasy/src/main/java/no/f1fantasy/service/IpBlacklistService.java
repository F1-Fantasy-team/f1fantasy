package no.f1fantasy.service;

import org.springframework.stereotype.Service;

import java.time.OffsetDateTime;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Service
public class IpBlacklistService {

    public static class BlacklistEntry {
        private final String ipAddress;
        private final String reason;
        private final OffsetDateTime blacklistedAt;
        private final OffsetDateTime expiresAt;

        public BlacklistEntry(String ipAddress, String reason, OffsetDateTime blacklistedAt, OffsetDateTime expiresAt) {
            this.ipAddress = ipAddress;
            this.reason = reason;
            this.blacklistedAt = blacklistedAt;
            this.expiresAt = expiresAt;
        }

        public String getIpAddress() {
            return ipAddress;
        }

        public String getReason() {
            return reason;
        }

        public OffsetDateTime getBlacklistedAt() {
            return blacklistedAt;
        }

        public OffsetDateTime getExpiresAt() {
            return expiresAt;
        }
    }

    private final ConcurrentHashMap<String, BlacklistEntry> blacklist = new ConcurrentHashMap<>();

    public boolean isBlacklisted(String ipAddress) {
        BlacklistEntry entry = blacklist.get(ipAddress);
        if (entry == null) {
            return false;
        }

        if (entry.getExpiresAt() != null && entry.getExpiresAt().isBefore(OffsetDateTime.now())) {
            blacklist.remove(ipAddress);
            return false;
        }

        return true;
    }

    public void blacklist(String ipAddress, String reason, java.time.Duration duration) {
        OffsetDateTime now = OffsetDateTime.now();
        OffsetDateTime expiresAt = duration == null ? null : now.plus(duration);
        blacklist.put(ipAddress, new BlacklistEntry(ipAddress, reason, now, expiresAt));
    }

    public void blacklist(String ipAddress, String reason) {
        blacklist(ipAddress, reason, null);
    }

    public void unblacklist(String ipAddress) {
        blacklist.remove(ipAddress);
    }

    public Map<String, BlacklistEntry> getBlacklistedIps() {
        blacklist.entrySet().removeIf(entry -> entry.getValue().getExpiresAt() != null && entry.getValue().getExpiresAt().isBefore(OffsetDateTime.now()));
        return Map.copyOf(blacklist);
    }
}

package no.f1fantasy.service;

import no.f1fantasy.entity.UserDisplayNameCache;
import no.f1fantasy.repository.UserDisplayNameCacheRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.time.OffsetDateTime;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ConcurrentHashMap;

@Service
public class ClerkService {

    private static final Duration MEMORY_CACHE_DURATION = Duration.ofMinutes(30);
    private static final Duration DB_CACHE_DURATION = Duration.ofDays(7);
    private static final Duration FAILED_LOOKUP_CACHE_DURATION = Duration.ofMinutes(5);

    private static class CacheEntry {
        private final String displayName;
        private final OffsetDateTime expiresAt;

        private CacheEntry(String displayName, OffsetDateTime expiresAt) {
            this.displayName = displayName;
            this.expiresAt = expiresAt;
        }
    }

    private final ConcurrentHashMap<String, CacheEntry> memoryCache = new ConcurrentHashMap<>();
    private final UserDisplayNameCacheRepository userDisplayNameCacheRepository;

    @Autowired
    public ClerkService(UserDisplayNameCacheRepository userDisplayNameCacheRepository) {
        this.userDisplayNameCacheRepository = userDisplayNameCacheRepository;
    }

    // Used by isolated unit tests that don't bootstrap Spring/JPA.
    ClerkService() {
        this.userDisplayNameCacheRepository = null;
    }

    public String getUserDisplayName(String userId) {
        if (userId == null || userId.isBlank()) {
            return "unknown-user";
        }

        CacheEntry cached = memoryCache.get(userId);
        if (cached != null && cached.expiresAt.isAfter(OffsetDateTime.now())) {
            return cached.displayName;
        }

        if (userDisplayNameCacheRepository == null) {
            String displayName = fetchDisplayName(userId);
            putMemoryCache(userId, displayName);
            return displayName;
        }

        Optional<UserDisplayNameCache> dbCached = userDisplayNameCacheRepository.findByUserId(userId);
        if (dbCached.isPresent()) {
            UserDisplayNameCache entry = dbCached.get();
            putMemoryCache(userId, entry.getDisplayName());

            if (entry.getExpiresAt() != null && entry.getExpiresAt().isBefore(OffsetDateTime.now())) {
                CompletableFuture.runAsync(() -> refreshAndPersist(userId));
            }

            return entry.getDisplayName();
        }

        return refreshAndPersist(userId);
    }

    public Map<String, String> getUserDisplayNames(List<String> userIds) {
        Map<String, String> result = new HashMap<>();
        if (userIds == null || userIds.isEmpty()) {
            return result;
        }

        for (String userId : userIds.stream().distinct().toList()) {
            result.put(userId, getUserDisplayName(userId));
        }

        return result;
    }

    private String refreshAndPersist(String userId) {
        try {
            String displayName = fetchDisplayName(userId);
            putMemoryCache(userId, displayName);
            persistDbCache(userId, displayName, DB_CACHE_DURATION);
            return displayName;
        } catch (Exception ex) {
            String fallback = userId;
            putMemoryCache(userId, fallback);
            persistDbCache(userId, fallback, FAILED_LOOKUP_CACHE_DURATION);
            return fallback;
        }
    }

    private void putMemoryCache(String userId, String displayName) {
        memoryCache.put(userId, new CacheEntry(displayName, OffsetDateTime.now().plus(MEMORY_CACHE_DURATION)));
    }

    private void persistDbCache(String userId, String displayName, Duration ttl) {
        if (userDisplayNameCacheRepository == null) {
            return;
        }

        OffsetDateTime now = OffsetDateTime.now();
        UserDisplayNameCache entity = userDisplayNameCacheRepository.findByUserId(userId)
            .orElseGet(UserDisplayNameCache::new);
        entity.setUserId(userId);
        entity.setDisplayName(displayName);
        entity.setCachedAt(now);
        entity.setExpiresAt(now.plus(ttl));
        userDisplayNameCacheRepository.save(entity);
    }

    private String fetchDisplayName(String userId) {
        // Placeholder until Clerk backend API integration is introduced in Java parity layer.
        // Mirrors .NET fallback behavior when no name details are available.
        return userId;
    }
}

package no.f1fantasy.repository;

import no.f1fantasy.entity.UserDisplayNameCache;
import org.springframework.data.jpa.repository.JpaRepository;

import java.time.OffsetDateTime;
import java.util.Optional;

public interface UserDisplayNameCacheRepository extends JpaRepository<UserDisplayNameCache, String> {
    Optional<UserDisplayNameCache> findByUserId(String userId);

    /** Used for cache cleanup — remove expired entries. */
    void deleteByExpiresAtBefore(OffsetDateTime threshold);
}

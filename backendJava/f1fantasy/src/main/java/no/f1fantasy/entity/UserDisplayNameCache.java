package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

import java.time.OffsetDateTime;

@Entity
@Table(name = "UserDisplayNameCache",
        indexes = {
                @Index(name = "IX_UserDisplayNameCache_ExpiresAt", columnList = "ExpiresAt")
        })
@Data
public class UserDisplayNameCache {

    @Id
    @Column(name = "UserId", length = 100)
    private String userId;

    @Column(name = "DisplayName", length = 200, nullable = false)
    private String displayName;

    @Column(name = "CachedAt", nullable = false)
    private OffsetDateTime cachedAt;

    @Column(name = "ExpiresAt", nullable = false)
    private OffsetDateTime expiresAt;
}

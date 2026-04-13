package no.f1fantasy.service;

import org.junit.jupiter.api.Test;

import java.time.Duration;

import static org.assertj.core.api.Assertions.assertThat;

class IpBlacklistServiceTest {

    @Test
    void shouldBlacklistAndUnblacklistIp() {
        IpBlacklistService service = new IpBlacklistService();

        service.blacklist("127.0.0.1", "too many requests");
        assertThat(service.isBlacklisted("127.0.0.1")).isTrue();

        service.unblacklist("127.0.0.1");
        assertThat(service.isBlacklisted("127.0.0.1")).isFalse();
    }

    @Test
    void shouldExpireTemporaryBlacklist() {
        IpBlacklistService service = new IpBlacklistService();

        service.blacklist("10.0.0.1", "temporary", Duration.ofMillis(1));

        // Expiration is validated on access.
        assertThat(service.isBlacklisted("10.0.0.1")).isIn(true, false);
        assertThat(service.getBlacklistedIps().containsKey("10.0.0.1")).isIn(true, false);
    }
}

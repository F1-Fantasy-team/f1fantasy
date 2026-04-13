package no.f1fantasy.service;

import org.junit.jupiter.api.Test;

import java.util.List;
import java.util.Map;

import static org.assertj.core.api.Assertions.assertThat;

class ClerkServiceTest {

    @Test
    void shouldReturnUserIdAsDisplayNameFallback() {
        ClerkService service = new ClerkService();

        String displayName = service.getUserDisplayName("user_123");

        assertThat(displayName).isEqualTo("user_123");
    }

    @Test
    void shouldReturnDistinctDisplayNamesForBatchLookup() {
        ClerkService service = new ClerkService();

        Map<String, String> names = service.getUserDisplayNames(List.of("u1", "u2", "u1"));

        assertThat(names).hasSize(2);
        assertThat(names.get("u1")).isEqualTo("u1");
        assertThat(names.get("u2")).isEqualTo("u2");
    }
}

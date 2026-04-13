package no.f1fantasy.entity.converter;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class ConverterTest {

    private final StringArrayConverter arrayConverter = new StringArrayConverter();
    private final StringListJsonConverter jsonConverter = new StringListJsonConverter();

    // ── StringArrayConverter ──────────────────────────────────────────

    @Test
    @DisplayName("StringArrayConverter: round-trip with typical season values")
    void arrayConverter_roundTrip() {
        List<String> input = List.of("2024", "2025", "2026");
        String db = arrayConverter.convertToDatabaseColumn(input);
        List<String> result = arrayConverter.convertToEntityAttribute(db);
        assertThat(result).containsExactlyInAnyOrder("2024", "2025", "2026");
    }

    @Test
    @DisplayName("StringArrayConverter: empty list produces {}")
    void arrayConverter_emptyList() {
        assertThat(arrayConverter.convertToDatabaseColumn(List.of())).isEqualTo("{}");
        assertThat(arrayConverter.convertToEntityAttribute("{}")).isEmpty();
    }

    @Test
    @DisplayName("StringArrayConverter: null returns empty list on read")
    void arrayConverter_nullRead() {
        assertThat(arrayConverter.convertToEntityAttribute(null)).isEmpty();
    }

    @Test
    @DisplayName("StringArrayConverter: parses PostgreSQL unquoted array literal")
    void arrayConverter_parsesUnquoted() {
        // PostgreSQL text[] can return unquoted simple values
        List<String> result = arrayConverter.convertToEntityAttribute("{hamilton,verstappen,leclerc}");
        assertThat(result).containsExactly("hamilton", "verstappen", "leclerc");
    }

    // ── StringListJsonConverter ───────────────────────────────────────

    @Test
    @DisplayName("StringListJsonConverter: round-trip with driver IDs")
    void jsonConverter_roundTrip() {
        List<String> input = List.of("verstappen", "hamilton", "leclerc");
        String db = jsonConverter.convertToDatabaseColumn(input);
        List<String> result = jsonConverter.convertToEntityAttribute(db);
        assertThat(result).containsExactly("verstappen", "hamilton", "leclerc");
    }

    @Test
    @DisplayName("StringListJsonConverter: empty list serialises to []")
    void jsonConverter_emptyList() {
        assertThat(jsonConverter.convertToDatabaseColumn(List.of())).isEqualTo("[]");
        assertThat(jsonConverter.convertToEntityAttribute("[]")).isEmpty();
    }

    @Test
    @DisplayName("StringListJsonConverter: null returns []")
    void jsonConverter_nullInput() {
        assertThat(jsonConverter.convertToDatabaseColumn(null)).isEqualTo("[]");
    }

    @Test
    @DisplayName("StringListJsonConverter: null DB value returns empty list")
    void jsonConverter_nullDbValue() {
        assertThat(jsonConverter.convertToEntityAttribute(null)).isEmpty();
    }
}

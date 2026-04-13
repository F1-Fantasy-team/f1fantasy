package no.f1fantasy.entity.converter;

import jakarta.persistence.AttributeConverter;
import jakarta.persistence.Converter;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.stream.Collectors;

/**
 * JPA converter for PostgreSQL {@code text[]} columns (e.g. {@code ActiveSeasons}).
 * <p>
 * Reads the PostgreSQL array literal format {@code {val1,val2,val3}} and writes
 * back in the same format.  In H2 (tests) the column becomes a VARCHAR and the
 * format is preserved for round-trip consistency.
 */
@Converter
public class StringArrayConverter implements AttributeConverter<List<String>, String> {

    @Override
    public String convertToDatabaseColumn(List<String> list) {
        if (list == null || list.isEmpty()) return "{}";
        String joined = list.stream()
                .map(s -> "\"" + s.replace("\\", "\\\\").replace("\"", "\\\"") + "\"")
                .collect(Collectors.joining(","));
        return "{" + joined + "}";
    }

    @Override
    public List<String> convertToEntityAttribute(String dbData) {
        if (dbData == null || dbData.isBlank() || "{}".equals(dbData.trim())) {
            return new ArrayList<>();
        }
        String trimmed = dbData.trim();
        if (trimmed.startsWith("{") && trimmed.endsWith("}")) {
            String inner = trimmed.substring(1, trimmed.length() - 1);
            if (inner.isBlank()) return new ArrayList<>();
            // Split on commas not inside double quotes
            List<String> result = new ArrayList<>();
            StringBuilder current = new StringBuilder();
            boolean inQuotes = false;
            for (int i = 0; i < inner.length(); i++) {
                char c = inner.charAt(i);
                if (c == '"') {
                    inQuotes = !inQuotes;
                } else if (c == ',' && !inQuotes) {
                    result.add(unquote(current.toString().trim()));
                    current = new StringBuilder();
                } else {
                    current.append(c);
                }
            }
            if (!current.toString().isBlank()) {
                result.add(unquote(current.toString().trim()));
            }
            return result;
        }
        // Fallback: treat as comma-separated (H2 test scenario)
        return new ArrayList<>(Arrays.asList(trimmed.split(",")));
    }

    private String unquote(String s) {
        if (s.startsWith("\"") && s.endsWith("\"")) {
            return s.substring(1, s.length() - 1).replace("\\\"", "\"").replace("\\\\", "\\");
        }
        return s;
    }
}

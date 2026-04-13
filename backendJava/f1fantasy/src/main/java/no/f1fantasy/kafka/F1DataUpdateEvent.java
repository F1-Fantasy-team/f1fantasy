package no.f1fantasy.kafka;

import no.f1fantasy.service.DataType;

import java.time.OffsetDateTime;
import java.util.Locale;
import java.util.Optional;

public class F1DataUpdateEvent {

    private String dataType;
    private String season;
    private String round;
    private OffsetDateTime updatedAt;
    private String source;

    public String getDataType() {
        return dataType;
    }

    public void setDataType(String dataType) {
        this.dataType = dataType;
    }

    public String getSeason() {
        return season;
    }

    public void setSeason(String season) {
        this.season = season;
    }

    public String getRound() {
        return round;
    }

    public void setRound(String round) {
        this.round = round;
    }

    public OffsetDateTime getUpdatedAt() {
        return updatedAt;
    }

    public void setUpdatedAt(OffsetDateTime updatedAt) {
        this.updatedAt = updatedAt;
    }

    public String getSource() {
        return source;
    }

    public void setSource(String source) {
        this.source = source;
    }

    public Optional<DataType> resolveDataType() {
        if (dataType == null || dataType.isBlank()) {
            return Optional.empty();
        }

        String normalized = dataType
            .trim()
            .replace('-', '_')
            .replace(' ', '_')
            .toUpperCase(Locale.ROOT);

        return switch (normalized) {
            case "RESULTS" -> Optional.of(DataType.RESULTS);
            case "QUALIFYING" -> Optional.of(DataType.QUALIFYING);
            case "DRIVER_STANDINGS", "DRIVERSTANDINGS" -> Optional.of(DataType.DRIVER_STANDINGS);
            case "CONSTRUCTOR_STANDINGS", "CONSTRUCTORSTANDINGS" -> Optional.of(DataType.CONSTRUCTOR_STANDINGS);
            case "RACES" -> Optional.of(DataType.RACES);
            case "CIRCUITS" -> Optional.of(DataType.CIRCUITS);
            case "DRIVERS" -> Optional.of(DataType.DRIVERS);
            case "CONSTRUCTORS" -> Optional.of(DataType.CONSTRUCTORS);
            default -> Optional.empty();
        };
    }
}

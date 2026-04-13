package no.f1fantasy.service;

public enum DataType {
    RESULTS,
    QUALIFYING,
    DRIVER_STANDINGS,
    CONSTRUCTOR_STANDINGS,
    RACES,
    CIRCUITS,
    DRIVERS,
    CONSTRUCTORS;

    public String toMetadataValue() {
        return switch (this) {
            case RESULTS -> "Results";
            case QUALIFYING -> "Qualifying";
            case DRIVER_STANDINGS -> "DriverStandings";
            case CONSTRUCTOR_STANDINGS -> "ConstructorStandings";
            case RACES -> "Races";
            case CIRCUITS -> "Circuits";
            case DRIVERS -> "Drivers";
            case CONSTRUCTORS -> "Constructors";
        };
    }
}

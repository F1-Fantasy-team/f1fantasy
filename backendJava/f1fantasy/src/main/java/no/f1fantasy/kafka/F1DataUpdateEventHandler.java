package no.f1fantasy.kafka;

import no.f1fantasy.service.CircuitService;
import no.f1fantasy.service.ConstructorService;
import no.f1fantasy.service.ConstructorStandingService;
import no.f1fantasy.service.DataType;
import no.f1fantasy.service.DriverService;
import no.f1fantasy.service.DriverStandingService;
import no.f1fantasy.service.QualifyingService;
import no.f1fantasy.service.RaceService;
import no.f1fantasy.service.ResultService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

@Service
public class F1DataUpdateEventHandler {

    private static final Logger logger = LoggerFactory.getLogger(F1DataUpdateEventHandler.class);

    private final ResultService resultService;
    private final QualifyingService qualifyingService;
    private final DriverStandingService driverStandingService;
    private final ConstructorStandingService constructorStandingService;
    private final RaceService raceService;
    private final CircuitService circuitService;
    private final DriverService driverService;
    private final ConstructorService constructorService;

    public F1DataUpdateEventHandler(
        ResultService resultService,
        QualifyingService qualifyingService,
        DriverStandingService driverStandingService,
        ConstructorStandingService constructorStandingService,
        RaceService raceService,
        CircuitService circuitService,
        DriverService driverService,
        ConstructorService constructorService
    ) {
        this.resultService = resultService;
        this.qualifyingService = qualifyingService;
        this.driverStandingService = driverStandingService;
        this.constructorStandingService = constructorStandingService;
        this.raceService = raceService;
        this.circuitService = circuitService;
        this.driverService = driverService;
        this.constructorService = constructorService;
    }

    public void handle(F1DataUpdateEvent event) {
        if (event == null) {
            logger.warn("Ignoring null Kafka update event");
            return;
        }

        DataType dataType = event.resolveDataType().orElse(null);
        if (dataType == null) {
            logger.warn("Ignoring Kafka update event with unsupported dataType '{}'", event.getDataType());
            return;
        }

        String season = event.getSeason();
        String round = event.getRound();

        logger.info(
            "Handling Kafka update event dataType={}, season={}, round={}, updatedAt={}, source={}",
            dataType,
            season,
            round,
            event.getUpdatedAt(),
            event.getSource()
        );

        switch (dataType) {
            case RESULTS -> {
                if (hasRound(season, round)) {
                    resultService.getResultsByRace(season, round);
                } else if (hasSeason(season)) {
                    resultService.getResultsBySeason(season);
                }
            }
            case QUALIFYING -> {
                if (hasRound(season, round)) {
                    qualifyingService.getQualifyingByRace(season, round);
                } else if (hasSeason(season)) {
                    qualifyingService.getQualifyingBySeason(season);
                }
            }
            case DRIVER_STANDINGS -> {
                if (hasRound(season, round)) {
                    driverStandingService.getDriverStandingsByRound(season, round);
                } else if (hasSeason(season)) {
                    driverStandingService.getDriverStandingsBySeason(season);
                }
            }
            case CONSTRUCTOR_STANDINGS -> {
                if (hasRound(season, round)) {
                    constructorStandingService.getConstructorStandingsByRound(season, round);
                } else if (hasSeason(season)) {
                    constructorStandingService.getConstructorStandingsBySeason(season);
                }
            }
            case RACES -> {
                if (hasSeason(season)) {
                    raceService.getRacesForSeason(season);
                }
            }
            case CIRCUITS -> circuitService.getAllCircuits();
            case DRIVERS -> {
                if (hasSeason(season)) {
                    driverService.getDriversBySeason(season);
                } else {
                    driverService.getAllDrivers();
                }
            }
            case CONSTRUCTORS -> {
                if (hasSeason(season)) {
                    constructorService.getConstructorsBySeason(season);
                } else {
                    constructorService.getAllConstructors();
                }
            }
            default -> logger.warn("No handler configured for dataType {}", dataType);
        }
    }

    private boolean hasSeason(String season) {
        return season != null && !season.isBlank();
    }

    private boolean hasRound(String season, String round) {
        return hasSeason(season) && round != null && !round.isBlank();
    }
}

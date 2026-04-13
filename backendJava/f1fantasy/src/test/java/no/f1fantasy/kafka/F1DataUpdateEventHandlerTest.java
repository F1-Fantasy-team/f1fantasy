package no.f1fantasy.kafka;

import no.f1fantasy.service.CircuitService;
import no.f1fantasy.service.ConstructorService;
import no.f1fantasy.service.ConstructorStandingService;
import no.f1fantasy.service.DriverService;
import no.f1fantasy.service.DriverStandingService;
import no.f1fantasy.service.QualifyingService;
import no.f1fantasy.service.RaceService;
import no.f1fantasy.service.ResultService;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.verifyNoInteractions;

@ExtendWith(MockitoExtension.class)
class F1DataUpdateEventHandlerTest {

    @Mock
    private ResultService resultService;

    @Mock
    private QualifyingService qualifyingService;

    @Mock
    private DriverStandingService driverStandingService;

    @Mock
    private ConstructorStandingService constructorStandingService;

    @Mock
    private RaceService raceService;

    @Mock
    private CircuitService circuitService;

    @Mock
    private DriverService driverService;

    @Mock
    private ConstructorService constructorService;

    @Test
    void shouldDispatchRoundResultsWhenRoundPresent() {
        F1DataUpdateEvent event = new F1DataUpdateEvent();
        event.setDataType("Results");
        event.setSeason("2026");
        event.setRound("3");

        createHandler().handle(event);

        verify(resultService).getResultsByRace("2026", "3");
        verify(resultService, never()).getResultsBySeason("2026");
    }

    @Test
    void shouldDispatchSeasonStandingsWhenRoundMissing() {
        F1DataUpdateEvent event = new F1DataUpdateEvent();
        event.setDataType("DRIVER_STANDINGS");
        event.setSeason("2026");

        createHandler().handle(event);

        verify(driverStandingService).getDriverStandingsBySeason("2026");
        verify(driverStandingService, never()).getDriverStandingsByRound("2026", "");
    }

    @Test
    void shouldDispatchDriversBySeasonWhenSeasonPresent() {
        F1DataUpdateEvent event = new F1DataUpdateEvent();
        event.setDataType("drivers");
        event.setSeason("2025");

        createHandler().handle(event);

        verify(driverService).getDriversBySeason("2025");
        verify(driverService, never()).getAllDrivers();
    }

    @Test
    void shouldDispatchAllConstructorsWhenSeasonMissing() {
        F1DataUpdateEvent event = new F1DataUpdateEvent();
        event.setDataType("constructors");

        createHandler().handle(event);

        verify(constructorService).getAllConstructors();
        verify(constructorService, never()).getConstructorsBySeason("2025");
    }

    @Test
    void shouldIgnoreUnknownDataType() {
        F1DataUpdateEvent event = new F1DataUpdateEvent();
        event.setDataType("unknown-event");
        event.setSeason("2026");

        createHandler().handle(event);

        verifyNoInteractions(
            resultService,
            qualifyingService,
            driverStandingService,
            constructorStandingService,
            raceService,
            circuitService,
            driverService,
            constructorService
        );
    }

    private F1DataUpdateEventHandler createHandler() {
        return new F1DataUpdateEventHandler(
            resultService,
            qualifyingService,
            driverStandingService,
            constructorStandingService,
            raceService,
            circuitService,
            driverService,
            constructorService
        );
    }
}

package no.f1fantasy.controller;

import java.util.List;

import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import no.f1fantasy.entity.Driver;
import no.f1fantasy.service.DriverService;

@RestController
@RequestMapping("/api/driver")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class DriverController {

    private final DriverService driverService;

    public DriverController(DriverService driverService) {
        this.driverService = driverService;
    }

    @GetMapping
    public ResponseEntity<?> getAllDrivers() {
        try {
            List<Driver> drivers = driverService.getAllDrivers();
            return ResponseEntity.ok(drivers);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(new ErrorResponse("Error fetching drivers"));
        }
    }

    @GetMapping("/season/{season}")
    public ResponseEntity<?> getDriversBySeason(@PathVariable String season) {
        try {
            List<Driver> drivers = driverService.getDriversBySeason(season);
            return ResponseEntity.ok(drivers);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(new ErrorResponse("Error fetching drivers for season: " + season));
        }
    }

    @GetMapping("/{driverId}")
    public ResponseEntity<?> getDriverById(@PathVariable String driverId) {
        try {
            java.util.Optional<Driver> driver = driverService.getDriverById(driverId);
            if (!driver.isPresent()) {
                return ResponseEntity.status(404).body(new ErrorResponse("Driver not found: " + driverId));
            }
            return ResponseEntity.ok(driver.get());
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(new ErrorResponse("Error fetching driver: " + driverId));
        }
    }

    @GetMapping("/cached")
    public ResponseEntity<?> getCachedDrivers() {
        try {
            List<Driver> drivers = driverService.getAllDrivers();
            if (drivers.isEmpty()) {
                return ResponseEntity.ok(new CachedResponse("No cached drivers found", drivers));
            }
            return ResponseEntity.ok(drivers);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(new ErrorResponse("Error fetching cached drivers"));
        }
    }

    @SuppressWarnings ("unused")
    private static class ErrorResponse {
        public final String error;

        public ErrorResponse(String error) {
            this.error = error;
        }
    }

    @SuppressWarnings("unused")
    private static class CachedResponse {
        public final String message;
        public final List<?> drivers;

        public CachedResponse(String message, List<?> drivers) {
            this.message = message;
            this.drivers = drivers;
        }
    }
}

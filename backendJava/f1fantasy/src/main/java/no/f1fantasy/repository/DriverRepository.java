package no.f1fantasy.repository;

import no.f1fantasy.entity.Driver;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface DriverRepository extends JpaRepository<Driver, String> {
    Optional<Driver> findByDriverId(String driverId);
}

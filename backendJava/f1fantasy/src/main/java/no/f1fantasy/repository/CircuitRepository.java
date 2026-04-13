package no.f1fantasy.repository;

import no.f1fantasy.entity.Circuit;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface CircuitRepository extends JpaRepository<Circuit, String> {
    Optional<Circuit> findByCircuitId(String circuitId);
}

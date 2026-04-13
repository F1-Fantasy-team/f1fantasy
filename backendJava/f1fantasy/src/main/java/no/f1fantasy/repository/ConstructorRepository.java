package no.f1fantasy.repository;

import no.f1fantasy.entity.Constructor;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface ConstructorRepository extends JpaRepository<Constructor, String> {
    Optional<Constructor> findByConstructorId(String constructorId);
}

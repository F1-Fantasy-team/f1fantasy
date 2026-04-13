package no.f1fantasy.repository;

import no.f1fantasy.entity.DriverChampionshipPrediction;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface DriverChampionshipPredictionRepository
        extends JpaRepository<DriverChampionshipPrediction, Integer> {
    Optional<DriverChampionshipPrediction> findByGroupIdAndUserId(Integer groupId, String userId);
}

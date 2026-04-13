package no.f1fantasy.repository;

import no.f1fantasy.entity.DriverDraftPrediction;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface DriverDraftPredictionRepository extends JpaRepository<DriverDraftPrediction, Integer> {
    Optional<DriverDraftPrediction> findByGroupIdAndUserId(Integer groupId, String userId);
}

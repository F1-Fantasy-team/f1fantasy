package no.f1fantasy.repository;

import no.f1fantasy.entity.DestructorPrediction;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface DestructorPredictionRepository extends JpaRepository<DestructorPrediction, Integer> {
    Optional<DestructorPrediction> findByGroupIdAndUserId(Integer groupId, String userId);
}

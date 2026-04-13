package no.f1fantasy.repository;

import no.f1fantasy.entity.MrSaturdayPrediction;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface MrSaturdayPredictionRepository extends JpaRepository<MrSaturdayPrediction, Integer> {
    Optional<MrSaturdayPrediction> findByGroupIdAndUserId(Integer groupId, String userId);
}

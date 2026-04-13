package no.f1fantasy.repository;

import no.f1fantasy.entity.ConstructorChampionshipPrediction;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface ConstructorChampionshipPredictionRepository
        extends JpaRepository<ConstructorChampionshipPrediction, Integer> {
    Optional<ConstructorChampionshipPrediction> findByGroupIdAndUserId(Integer groupId, String userId);
}

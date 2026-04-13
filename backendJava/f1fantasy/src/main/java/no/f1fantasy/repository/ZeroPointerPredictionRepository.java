package no.f1fantasy.repository;

import no.f1fantasy.entity.ZeroPointerPrediction;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface ZeroPointerPredictionRepository extends JpaRepository<ZeroPointerPrediction, Integer> {
    Optional<ZeroPointerPrediction> findByGroupIdAndUserId(Integer groupId, String userId);
}

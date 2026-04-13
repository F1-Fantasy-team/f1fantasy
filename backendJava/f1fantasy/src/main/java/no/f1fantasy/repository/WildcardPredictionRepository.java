package no.f1fantasy.repository;

import no.f1fantasy.entity.WildcardPrediction;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface WildcardPredictionRepository extends JpaRepository<WildcardPrediction, Integer> {
    Optional<WildcardPrediction> findByGroupIdAndUserId(Integer groupId, String userId);
    List<WildcardPrediction> findByGroupId(Integer groupId);
}

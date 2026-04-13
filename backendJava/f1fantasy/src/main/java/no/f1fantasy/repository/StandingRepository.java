package no.f1fantasy.repository;

import no.f1fantasy.entity.Standing;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface StandingRepository extends JpaRepository<Standing, Integer> {
    List<Standing> findByGroupIdOrderByRankAsc(Integer groupId);
    Optional<Standing> findByGroupIdAndUserId(Integer groupId, String userId);
    void deleteByGroupId(Integer groupId);
}

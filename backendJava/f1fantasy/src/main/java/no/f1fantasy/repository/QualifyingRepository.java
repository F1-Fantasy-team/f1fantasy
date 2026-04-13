package no.f1fantasy.repository;

import no.f1fantasy.entity.Qualifying;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface QualifyingRepository extends JpaRepository<Qualifying, Integer> {
    List<Qualifying> findBySeason(String season);
    List<Qualifying> findBySeasonAndRound(String season, String round);
    boolean existsBySeasonAndRound(String season, String round);
}

package no.f1fantasy.repository;

import no.f1fantasy.entity.LapTiming;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface LapTimingRepository extends JpaRepository<LapTiming, Integer> {
    List<LapTiming> findBySeasonAndRound(String season, String round);
    boolean existsBySeasonAndRound(String season, String round);
}

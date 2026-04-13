package no.f1fantasy.repository;

import no.f1fantasy.entity.PitStop;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface PitStopRepository extends JpaRepository<PitStop, Integer> {
    List<PitStop> findBySeasonAndRound(String season, String round);
    List<PitStop> findByDriverIdAndSeason(String driverId, String season);
    boolean existsBySeasonAndRound(String season, String round);
}

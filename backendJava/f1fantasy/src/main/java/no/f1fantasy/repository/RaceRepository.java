package no.f1fantasy.repository;

import no.f1fantasy.entity.Race;
import no.f1fantasy.entity.RaceId;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface RaceRepository extends JpaRepository<Race, RaceId> {
    List<Race> findBySeason(String season);
    Optional<Race> findBySeasonAndRound(String season, String round);

    /** Used by AutoLockScheduler — fetch first round of a given year ordered by round. */
    List<Race> findBySeasonOrderByRoundAsc(String season);
}

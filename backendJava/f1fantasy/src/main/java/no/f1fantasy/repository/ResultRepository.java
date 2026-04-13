package no.f1fantasy.repository;

import no.f1fantasy.entity.Result;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface ResultRepository extends JpaRepository<Result, Integer> {
    List<Result> findBySeason(String season);
    List<Result> findBySeasonAndRound(String season, String round);
    List<Result> findBySeasonAndRoundAndIsSprint(String season, String round, boolean isSprint);
    Optional<Result> findBySeasonAndRoundAndDriverId(String season, String round, String driverId);
    List<Result> findBySeasonAndDriverId(String season, String driverId);
}

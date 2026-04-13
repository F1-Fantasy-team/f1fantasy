package no.f1fantasy.repository;

import no.f1fantasy.entity.DriverStanding;
import no.f1fantasy.entity.DriverStandingId;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;
import java.util.Optional;

public interface DriverStandingRepository extends JpaRepository<DriverStanding, DriverStandingId> {
    List<DriverStanding> findBySeason(String season);

    /** Latest round standings for a season (all drivers). */
    @Query("SELECT ds FROM DriverStanding ds WHERE ds.season = :season AND ds.round = " +
           "(SELECT MAX(ds2.round) FROM DriverStanding ds2 WHERE ds2.season = :season)")
    List<DriverStanding> findLatestBySeason(@Param("season") String season);

    /** Standings at a specific round. */
    List<DriverStanding> findBySeasonAndRound(String season, String round);

    Optional<DriverStanding> findBySeasonAndDriverId(String season, String driverId);
}

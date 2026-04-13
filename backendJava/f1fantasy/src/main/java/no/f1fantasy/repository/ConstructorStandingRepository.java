package no.f1fantasy.repository;

import no.f1fantasy.entity.ConstructorStanding;
import no.f1fantasy.entity.ConstructorStandingId;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;

public interface ConstructorStandingRepository extends JpaRepository<ConstructorStanding, ConstructorStandingId> {
    List<ConstructorStanding> findBySeason(String season);

    @Query("SELECT cs FROM ConstructorStanding cs WHERE cs.season = :season AND cs.round = " +
           "(SELECT MAX(cs2.round) FROM ConstructorStanding cs2 WHERE cs2.season = :season)")
    List<ConstructorStanding> findLatestBySeason(@Param("season") String season);

    List<ConstructorStanding> findBySeasonAndRound(String season, String round);
}

package no.f1fantasy.repository;

import no.f1fantasy.entity.Season;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface SeasonRepository extends JpaRepository<Season, String> {
    Optional<Season> findByYear(String year);
}

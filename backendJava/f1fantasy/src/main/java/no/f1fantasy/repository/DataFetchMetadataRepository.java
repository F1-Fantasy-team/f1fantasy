package no.f1fantasy.repository;

import no.f1fantasy.entity.DataFetchMetadata;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface DataFetchMetadataRepository extends JpaRepository<DataFetchMetadata, Integer> {
    Optional<DataFetchMetadata> findBySeasonAndDataType(String season, String dataType);
    boolean existsBySeasonAndDataType(String season, String dataType);
}

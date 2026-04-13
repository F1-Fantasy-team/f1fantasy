package no.f1fantasy.service;

import no.f1fantasy.entity.DataFetchMetadata;
import no.f1fantasy.entity.Race;
import no.f1fantasy.repository.DataFetchMetadataRepository;
import no.f1fantasy.repository.RaceRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.Duration;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class CacheStalenessServiceTest {

    @Mock
    private DataFetchMetadataRepository metadataRepository;

    @Mock
    private RaceRepository raceRepository;

    private CacheStalenessService service;

    @SuppressWarnings("unused")
    @BeforeEach
    void setUp() {
        service = new CacheStalenessService(metadataRepository, raceRepository);
    }

    @Test
    void shouldFetchWhenMetadataMissing() {
        when(metadataRepository.findBySeasonAndDataType("2025", "Results"))
            .thenReturn(Optional.empty());

        boolean shouldFetch = service.shouldFetch("2025", DataType.RESULTS);

        assertThat(shouldFetch).isTrue();
    }

    @Test
    void shouldFetchWhenMetadataFetchWasNotSuccessful() {
        DataFetchMetadata metadata = new DataFetchMetadata();
        metadata.setFetchSuccessful(false);

        when(metadataRepository.findBySeasonAndDataType("2025", "Results"))
            .thenReturn(Optional.of(metadata));

        boolean shouldFetch = service.shouldFetch("2025", DataType.RESULTS);

        assertThat(shouldFetch).isTrue();
    }

    @Test
    void shouldFetchWhenCacheExpired() {
        DataFetchMetadata metadata = new DataFetchMetadata();
        metadata.setFetchSuccessful(true);
        metadata.setLastFetchedAt(OffsetDateTime.now().minusDays(10));

        when(metadataRepository.findBySeasonAndDataType("2025", "Results"))
            .thenReturn(Optional.of(metadata));

        boolean shouldFetch = service.shouldFetch("2025", DataType.RESULTS);

        assertThat(shouldFetch).isTrue();
    }

    @Test
    void shouldFetchWhenRaceHappenedAfterLastFetch() {
        DataFetchMetadata metadata = new DataFetchMetadata();
        metadata.setFetchSuccessful(true);
        metadata.setLastFetchedAt(OffsetDateTime.now().minusDays(2));

        Race race = new Race();
        race.setDate(OffsetDateTime.now().minusDays(1).toLocalDate().toString());

        CacheStalenessOptions options = new CacheStalenessOptions();
        options.setCurrentSeasonExpiration(Duration.ofHours(6));
        options.setRaceDataAvailabilityBuffer(Duration.ofDays(1));

        when(metadataRepository.findBySeasonAndDataType("2025", "Results"))
            .thenReturn(Optional.of(metadata));
        when(raceRepository.findBySeason("2025")).thenReturn(List.of(race));

        boolean shouldFetch = service.shouldFetch("2025", DataType.RESULTS, options);

        assertThat(shouldFetch).isTrue();
    }

    @Test
    void shouldSkipFetchWhenCacheFreshAndNoNewRace() {
        DataFetchMetadata metadata = new DataFetchMetadata();
        metadata.setFetchSuccessful(true);
        metadata.setLastFetchedAt(OffsetDateTime.now().minusMinutes(10));

        Race race = new Race();
        race.setDate(OffsetDateTime.now().minusDays(2).toLocalDate().toString());

        CacheStalenessOptions options = new CacheStalenessOptions();
        options.setCurrentSeasonExpiration(Duration.ofHours(2));

        when(metadataRepository.findBySeasonAndDataType("2025", "Results"))
            .thenReturn(Optional.of(metadata));
        when(raceRepository.findBySeason("2025")).thenReturn(List.of(race));

        boolean shouldFetch = service.shouldFetch("2025", DataType.RESULTS, options);

        assertThat(shouldFetch).isFalse();
    }
}

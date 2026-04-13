package no.f1fantasy.service;

import no.f1fantasy.entity.DataFetchMetadata;
import no.f1fantasy.entity.Race;
import no.f1fantasy.repository.DataFetchMetadataRepository;
import no.f1fantasy.repository.RaceRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.List;
import java.util.Optional;

@Service
public class CacheStalenessService {

    private static final Logger logger = LoggerFactory.getLogger(CacheStalenessService.class);

    private final DataFetchMetadataRepository metadataRepository;
    private final RaceRepository raceRepository;

    public CacheStalenessService(DataFetchMetadataRepository metadataRepository, RaceRepository raceRepository) {
        this.metadataRepository = metadataRepository;
        this.raceRepository = raceRepository;
    }

    public boolean shouldFetch(String season, DataType dataType) {
        return shouldFetch(season, dataType, CacheStalenessOptions.DEFAULT);
    }

    public boolean shouldFetch(String season, DataType dataType, CacheStalenessOptions options) {
        CacheStalenessOptions resolvedOptions = options == null ? CacheStalenessOptions.DEFAULT : options;

        Optional<DataFetchMetadata> metadataOpt =
            metadataRepository.findBySeasonAndDataType(season, dataType.toMetadataValue());

        if (metadataOpt.isEmpty() || !metadataOpt.get().isFetchSuccessful()) {
            logger.debug("No valid metadata for {}/{}; should fetch", dataType, season);
            return true;
        }

        DataFetchMetadata metadata = metadataOpt.get();
        int currentYear = OffsetDateTime.now(ZoneOffset.UTC).getYear();
        int seasonYear = Integer.parseInt(season);

        OffsetDateTime expirationBoundary = metadata.getLastFetchedAt().plus(
            seasonYear < currentYear
                ? resolvedOptions.getPastSeasonExpiration()
                : resolvedOptions.getCurrentSeasonExpiration()
        );

        if (OffsetDateTime.now(ZoneOffset.UTC).isAfter(expirationBoundary)) {
            logger.debug("Cache expired for {}/{}; should fetch", dataType, season);
            return true;
        }

        if (!resolvedOptions.isCheckRaceSchedule()) {
            return false;
        }

        List<Race> races = raceRepository.findBySeason(season);
        OffsetDateTime nowPlusBuffer = OffsetDateTime.now(ZoneOffset.UTC)
            .plus(resolvedOptions.getRaceDataAvailabilityBuffer());

        for (Race race : races) {
            OffsetDateTime raceDate = tryParseRaceDate(race.getDate());
            if (raceDate == null) {
                continue;
            }

            if (raceDate.isAfter(metadata.getLastFetchedAt()) && raceDate.isBefore(nowPlusBuffer)) {
                logger.info("Race found after last fetch for {}/{}; should fetch", dataType, season);
                return true;
            }
        }

        logger.debug("Cache still valid for {}/{}; skip fetch", dataType, season);
        return false;
    }

    private OffsetDateTime tryParseRaceDate(String date) {
        if (date == null || date.isBlank()) {
            return null;
        }
        try {
            LocalDate localDate = LocalDate.parse(date);
            return localDate.atStartOfDay().atOffset(ZoneOffset.UTC);
        } catch (Exception ignored) {
            return null;
        }
    }
}

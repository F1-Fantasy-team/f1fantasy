package no.f1fantasy.service;

import no.f1fantasy.entity.Group;
import no.f1fantasy.entity.Race;
import no.f1fantasy.repository.GroupRepository;
import no.f1fantasy.repository.RaceRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;

import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.List;

@Service
public class AutoLockService {

    private static final Logger logger = LoggerFactory.getLogger(AutoLockService.class);

    private final GroupRepository groupRepository;
    private final RaceRepository raceRepository;

    public AutoLockService(GroupRepository groupRepository, RaceRepository raceRepository) {
        this.groupRepository = groupRepository;
        this.raceRepository = raceRepository;
    }

    @Scheduled(fixedDelay = 300000)
    public void checkAndLockGroups() {
        checkAndLockGroupsNow();
    }

    public int checkAndLockGroupsNow() {
        String currentSeason = String.valueOf(OffsetDateTime.now(ZoneOffset.UTC).getYear());
        List<Race> races = raceRepository.findBySeasonOrderByRoundAsc(currentSeason);
        if (races.isEmpty()) {
            return 0;
        }

        Race firstRace = races.getFirst();
        if (firstRace.getDate() == null) {
            return 0;
        }

        LocalDate firstRaceDate;
        try {
            firstRaceDate = LocalDate.parse(firstRace.getDate());
        } catch (Exception ex) {
            logger.warn("Failed to parse first race date '{}' for season {}", firstRace.getDate(), currentSeason);
            return 0;
        }

        if (LocalDate.now(ZoneOffset.UTC).isBefore(firstRaceDate)) {
            return 0;
        }

        List<Group> groupsToLock = groupRepository.findByLockModeInAndPredictionsLockedFalse(List.of("system", "hybrid"));
        for (Group group : groupsToLock) {
            group.setPredictionsLocked(true);
            group.setLockedAt(OffsetDateTime.now());
        }

        if (!groupsToLock.isEmpty()) {
            groupRepository.saveAll(groupsToLock);
        }

        return groupsToLock.size();
    }
}

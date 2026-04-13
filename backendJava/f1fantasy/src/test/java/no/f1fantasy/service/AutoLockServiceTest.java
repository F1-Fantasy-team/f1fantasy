package no.f1fantasy.service;

import no.f1fantasy.entity.Group;
import no.f1fantasy.entity.Race;
import no.f1fantasy.repository.GroupRepository;
import no.f1fantasy.repository.RaceRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.LocalDate;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.anyList;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@SuppressWarnings("null")
class AutoLockServiceTest {

    @Mock
    private GroupRepository groupRepository;

    @Mock
    private RaceRepository raceRepository;

    @Test
    void shouldLockEligibleGroupsAfterSeasonStart() {
        String currentSeason = String.valueOf(LocalDate.now().getYear());

        Race firstRace = new Race();
        firstRace.setSeason(currentSeason);
        firstRace.setRound("1");
        firstRace.setDate(LocalDate.now().minusDays(1).toString());

        Group group = new Group();
        group.setId(1);
        group.setLockMode("system");
        group.setPredictionsLocked(false);

        when(raceRepository.findBySeasonOrderByRoundAsc(currentSeason)).thenReturn(List.of(firstRace));
        when(groupRepository.findByLockModeInAndPredictionsLockedFalse(List.of("system", "hybrid"))).thenReturn(List.of(group));

        AutoLockService service = new AutoLockService(groupRepository, raceRepository);
        int locked = service.checkAndLockGroupsNow();

        assertThat(locked).isEqualTo(1);
        verify(groupRepository, times(1)).saveAll(anyList());
    }
}

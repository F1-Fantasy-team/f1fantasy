package no.f1fantasy.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.entity.Group;
import no.f1fantasy.entity.GroupMember;
import no.f1fantasy.entity.Standing;
import no.f1fantasy.repository.GroupMemberRepository;
import no.f1fantasy.repository.GroupRepository;
import no.f1fantasy.repository.StandingRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;
import java.util.Map;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.anyList;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@SuppressWarnings("null")
class StandingsServiceTest {

    @Mock
    private StandingRepository standingRepository;

    @Mock
    private GroupRepository groupRepository;

    @Mock
    private GroupMemberRepository groupMemberRepository;

    @Mock
    private ScoringService scoringService;

    @Test
    void shouldRecalculateAndPersistRankedStandings() {
        Group group = new Group();
        group.setId(10);

        GroupMember m1 = new GroupMember();
        m1.setGroupId(10);
        m1.setUserId("u1");

        GroupMember m2 = new GroupMember();
        m2.setGroupId(10);
        m2.setUserId("u2");

        when(groupRepository.findById(10)).thenReturn(Optional.of(group));
        when(groupMemberRepository.findByGroupId(10)).thenReturn(List.of(m1, m2));
        when(standingRepository.findByGroupIdAndUserId(10, "u1")).thenReturn(Optional.empty());
        when(standingRepository.findByGroupIdAndUserId(10, "u2")).thenReturn(Optional.empty());

        when(scoringService.calculateAllCategoryScores(10, "u1", "2025")).thenReturn(Map.of("driverDraft", 100, "wildcard", 20));
        when(scoringService.calculateAllCategoryScores(10, "u2", "2025")).thenReturn(Map.of("driverDraft", 80, "wildcard", 10));

        StandingsService service = new StandingsService(
            standingRepository,
            groupRepository,
            groupMemberRepository,
            scoringService,
            new ObjectMapper());

        service.recalculateStandings(10, "2025");

        verify(standingRepository, times(1)).saveAll(anyList());
    }

    @Test
    void shouldReturnUserStandingWhenPresent() {
        Standing standing = new Standing();
        standing.setGroupId(11);
        standing.setUserId("u11");
        standing.setRank(1);

        when(standingRepository.findByGroupIdAndUserId(11, "u11")).thenReturn(Optional.of(standing));

        StandingsService service = new StandingsService(
            standingRepository,
            groupRepository,
            groupMemberRepository,
            scoringService,
            new ObjectMapper());

        Optional<Standing> found = service.getUserStanding(11, "u11");

        assertThat(found).isPresent();
        assertThat(found.get().getRank()).isEqualTo(1);
    }
}

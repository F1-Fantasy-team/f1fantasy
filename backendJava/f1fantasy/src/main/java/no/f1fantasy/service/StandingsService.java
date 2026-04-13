package no.f1fantasy.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.entity.Group;
import no.f1fantasy.entity.GroupMember;
import no.f1fantasy.entity.Standing;
import no.f1fantasy.repository.GroupMemberRepository;
import no.f1fantasy.repository.GroupRepository;
import no.f1fantasy.repository.StandingRepository;
import org.springframework.stereotype.Service;

import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.NoSuchElementException;
import java.util.Optional;

@Service
public class StandingsService {

    private final StandingRepository standingRepository;
    private final GroupRepository groupRepository;
    private final GroupMemberRepository groupMemberRepository;
    private final ScoringService scoringService;
    private final ObjectMapper objectMapper;

    public StandingsService(
        StandingRepository standingRepository,
        GroupRepository groupRepository,
        GroupMemberRepository groupMemberRepository,
        ScoringService scoringService,
        ObjectMapper objectMapper
    ) {
        this.standingRepository = standingRepository;
        this.groupRepository = groupRepository;
        this.groupMemberRepository = groupMemberRepository;
        this.scoringService = scoringService;
        this.objectMapper = objectMapper;
    }

    public List<Standing> getStandings(Integer groupId) {
        return standingRepository.findByGroupIdOrderByRankAsc(groupId);
    }

    public List<Standing> getStandingsWithAutoRecalc(Integer groupId, String season) {
        recalculateStandings(groupId, season);
        return standingRepository.findByGroupIdOrderByRankAsc(groupId);
    }

    public void recalculateStandings(Integer groupId, String season) {
        Integer safeGroupId = java.util.Objects.requireNonNull(groupId, "groupId must not be null");
        Group group = groupRepository.findById(safeGroupId)
            .orElseThrow(() -> new NoSuchElementException("Group not found"));

        scoringService.ensureSeasonDataAvailable(season);

        List<GroupMember> members = groupMemberRepository.findByGroupId(group.getId());
        List<Standing> standings = new ArrayList<>();

        for (GroupMember member : members) {
            Map<String, Integer> categoryScores = scoringService.calculateAllCategoryScores(safeGroupId, member.getUserId(), season);
            int total = categoryScores.values().stream().mapToInt(Integer::intValue).sum();

            Standing standing = standingRepository.findByGroupIdAndUserId(safeGroupId, member.getUserId())
                .orElseGet(Standing::new);

            standing.setGroupId(safeGroupId);
            standing.setUserId(member.getUserId());
            standing.setTotalScore(total);
            standing.setCategoryScoresJson(serializeCategoryScores(categoryScores));
            standing.setUpdatedAt(OffsetDateTime.now());
            standings.add(standing);
        }

        standings.sort(Comparator
            .comparingInt(Standing::getTotalScore)
            .reversed()
            .thenComparing(Standing::getUserId));

        for (int i = 0; i < standings.size(); i++) {
            standings.get(i).setRank(i + 1);
        }

        standingRepository.saveAll(standings);
    }

    public Optional<Standing> getUserStanding(Integer groupId, String userId) {
        return standingRepository.findByGroupIdAndUserId(groupId, userId);
    }

    private String serializeCategoryScores(Map<String, Integer> categoryScores) {
        try {
            return objectMapper.writeValueAsString(categoryScores);
        } catch (JsonProcessingException ex) {
            throw new IllegalStateException("Failed to serialize category scores", ex);
        }
    }
}

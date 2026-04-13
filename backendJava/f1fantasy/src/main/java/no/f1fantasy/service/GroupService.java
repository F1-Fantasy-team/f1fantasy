package no.f1fantasy.service;

import no.f1fantasy.entity.Group;
import no.f1fantasy.entity.GroupMember;
import no.f1fantasy.kafka.EventPublisher;
import no.f1fantasy.kafka.event.GroupEvent;
import no.f1fantasy.repository.GroupMemberRepository;
import no.f1fantasy.repository.GroupRepository;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.time.OffsetDateTime;
import java.util.List;
import java.util.NoSuchElementException;
import java.util.Objects;
import java.util.Optional;
import java.util.concurrent.ThreadLocalRandom;

@Service
public class GroupService {

    private static final String INVITE_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private final GroupRepository groupRepository;
    private final GroupMemberRepository groupMemberRepository;
    private final EventPublisher eventPublisher;
    private final String groupEventsTopic;

    public GroupService(
        GroupRepository groupRepository,
        GroupMemberRepository groupMemberRepository,
        EventPublisher eventPublisher,
        @Value("${app.kafka.topics.group-events:f1.group.events}") String groupEventsTopic
    ) {
        this.groupRepository = groupRepository;
        this.groupMemberRepository = groupMemberRepository;
        this.eventPublisher = eventPublisher;
        this.groupEventsTopic = groupEventsTopic;
    }

    public Group createGroup(String name, String adminUserId, String lockMode) {
        validateGroupName(name);
        String safeAdminUserId = Objects.requireNonNull(adminUserId, "adminUserId must not be null");
        String safeLockMode = normalizeAndValidateLockMode(lockMode);

        Group group = new Group();
        group.setName(name.trim());
        group.setAdminUserId(safeAdminUserId);
        group.setLockMode(safeLockMode);
        group.setInviteCode(generateUniqueInviteCode());
        group.setCreatedAt(OffsetDateTime.now());
        group.setPredictionsLocked(false);

        Group created = groupRepository.save(group);

        GroupMember adminMembership = new GroupMember();
        adminMembership.setGroupId(created.getId());
        adminMembership.setUserId(safeAdminUserId);
        adminMembership.setJoinedAt(OffsetDateTime.now());
        groupMemberRepository.save(adminMembership);

        publishGroupEvent("GROUP_CREATED", created, safeAdminUserId, safeAdminUserId);

        return created;
    }

    public Optional<Group> getGroupById(Integer id) {
        Integer safeId = Objects.requireNonNull(id, "id must not be null");
        return groupRepository.findById(safeId);
    }

    public Optional<Group> getGroupByInviteCode(String inviteCode) {
        return groupRepository.findByInviteCode(Objects.requireNonNull(inviteCode, "inviteCode must not be null"));
    }

    public List<Group> getUserGroups(String userId) {
        return groupRepository.findByMembers_UserId(Objects.requireNonNull(userId, "userId must not be null"));
    }

    public GroupMember joinGroup(Integer groupId, String userId) {
        String safeUserId = Objects.requireNonNull(userId, "userId must not be null");

        if (groupMemberRepository.existsByGroupIdAndUserId(groupId, safeUserId)) {
            throw new IllegalStateException("User is already a member of this group");
        }

        GroupMember member = new GroupMember();
        member.setGroupId(groupId);
        member.setUserId(safeUserId);
        member.setJoinedAt(OffsetDateTime.now());
        GroupMember saved = groupMemberRepository.save(member);

        Group group = findExistingGroup(groupId);
        publishGroupEvent("GROUP_MEMBER_JOINED", group, safeUserId, safeUserId);
        return saved;
    }

    public void leaveGroup(Integer groupId, String userId) {
        Group group = findExistingGroup(groupId);
        String safeUserId = Objects.requireNonNull(userId, "userId must not be null");

        if (safeUserId.equals(group.getAdminUserId())) {
            throw new IllegalStateException("Admin cannot leave the group");
        }

        groupMemberRepository.deleteByGroupIdAndUserId(groupId, safeUserId);
        publishGroupEvent("GROUP_MEMBER_LEFT", group, safeUserId, safeUserId);
    }

    public Group renameGroup(Integer groupId, String userId, String newName) {
        Group group = findExistingGroup(groupId);
        enforceAdmin(group, userId);
        validateGroupName(newName);

        group.setName(newName.trim());
        Group saved = groupRepository.save(group);
        publishGroupEvent("GROUP_RENAMED", saved, userId, null);
        return saved;
    }

    public void removeMember(Integer groupId, String adminUserId, String targetUserId) {
        Group group = findExistingGroup(groupId);
        enforceAdmin(group, adminUserId);
        String safeTargetUserId = Objects.requireNonNull(targetUserId, "targetUserId must not be null");

        if (safeTargetUserId.equals(group.getAdminUserId())) {
            throw new IllegalStateException("Cannot remove admin from group");
        }

        if (!groupMemberRepository.existsByGroupIdAndUserId(groupId, safeTargetUserId)) {
            throw new IllegalStateException("User is not a member of this group");
        }

        groupMemberRepository.deleteByGroupIdAndUserId(groupId, safeTargetUserId);
        publishGroupEvent("GROUP_MEMBER_REMOVED", group, adminUserId, safeTargetUserId);
    }

    public void deleteGroup(Integer groupId, String userId) {
        Group group = findExistingGroup(groupId);
        enforceAdmin(group, userId);

        groupMemberRepository.deleteByGroupId(groupId);
        groupRepository.delete(Objects.requireNonNull(group, "group must not be null"));
        publishGroupEvent("GROUP_DELETED", group, userId, null);
    }

    public Group lockPredictions(Integer groupId, String userId) {
        Group group = findExistingGroup(groupId);

        if ("admin".equals(group.getLockMode()) && !Objects.equals(group.getAdminUserId(), userId)) {
            throw new SecurityException("Only admin can lock predictions in admin mode");
        }

        group.setPredictionsLocked(true);
        group.setLockedAt(OffsetDateTime.now());
        Group saved = groupRepository.save(group);
        publishGroupEvent("GROUP_PREDICTIONS_LOCKED", saved, userId, null);
        return saved;
    }

    public Group unlockPredictions(Integer groupId, String userId) {
        Group group = findExistingGroup(groupId);

        if ("admin".equals(group.getLockMode()) && !Objects.equals(group.getAdminUserId(), userId)) {
            throw new SecurityException("Only admin can unlock predictions in admin mode");
        }

        if ("system".equals(group.getLockMode())) {
            throw new IllegalStateException("Cannot manually unlock in system mode");
        }

        group.setPredictionsLocked(false);
        group.setLockedAt(null);
        Group saved = groupRepository.save(group);
        publishGroupEvent("GROUP_PREDICTIONS_UNLOCKED", saved, userId, null);
        return saved;
    }

    private void publishGroupEvent(String eventType, Group group, String actorUserId, String targetUserId) {
        eventPublisher.publish(
            groupEventsTopic,
            String.valueOf(group.getId()),
            new GroupEvent(
                eventType,
                group.getId(),
                actorUserId,
                targetUserId,
                group.getName(),
                group.getLockMode(),
                group.isPredictionsLocked(),
                OffsetDateTime.now()
            )
        );
    }

    private Group findExistingGroup(Integer groupId) {
        Integer safeGroupId = Objects.requireNonNull(groupId, "groupId must not be null");
        return groupRepository.findById(safeGroupId)
            .orElseThrow(() -> new NoSuchElementException("Group not found"));
    }

    private void enforceAdmin(Group group, String userId) {
        if (!Objects.equals(group.getAdminUserId(), userId)) {
            throw new SecurityException("Only admin can perform this operation");
        }
    }

    private String generateUniqueInviteCode() {
        for (int attempt = 0; attempt < 20; attempt++) {
            String candidate = randomInviteCode(8);
            if (groupRepository.findByInviteCode(candidate).isEmpty()) {
                return candidate;
            }
        }

        throw new IllegalStateException("Unable to generate unique invite code");
    }

    private String randomInviteCode(int length) {
        StringBuilder builder = new StringBuilder(length);
        for (int i = 0; i < length; i++) {
            int index = ThreadLocalRandom.current().nextInt(INVITE_CHARS.length());
            builder.append(INVITE_CHARS.charAt(index));
        }
        return builder.toString();
    }

    private void validateGroupName(String name) {
        String safeName = Objects.requireNonNull(name, "name must not be null").trim();
        if (safeName.isEmpty()) {
            throw new IllegalArgumentException("Group name must not be empty");
        }
        if (safeName.length() > 200) {
            throw new IllegalArgumentException("Group name must be at most 200 characters");
        }
    }

    private String normalizeAndValidateLockMode(String lockMode) {
        String safeLockMode = Objects.requireNonNull(lockMode, "lockMode must not be null").trim().toLowerCase();
        if (!List.of("admin", "system", "hybrid").contains(safeLockMode)) {
            throw new IllegalArgumentException("Invalid lock mode: " + lockMode);
        }
        return safeLockMode;
    }
}

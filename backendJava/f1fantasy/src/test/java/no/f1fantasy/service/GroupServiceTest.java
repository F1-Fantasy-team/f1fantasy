package no.f1fantasy.service;

import no.f1fantasy.entity.Group;
import no.f1fantasy.entity.GroupMember;
import no.f1fantasy.kafka.EventPublisher;
import no.f1fantasy.repository.GroupMemberRepository;
import no.f1fantasy.repository.GroupRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@SuppressWarnings("null")
class GroupServiceTest {

    @Mock
    private GroupRepository groupRepository;

    @Mock
    private GroupMemberRepository groupMemberRepository;

    @Mock
    private EventPublisher eventPublisher;

    @Test
    void shouldCreateGroupAndAddAdminAsMember() {
        Group saved = new Group();
        saved.setId(10);
        saved.setName("Champions Club");
        saved.setAdminUserId("admin-1");
        saved.setLockMode("admin");
        saved.setInviteCode("ABCDEFGH");

        when(groupRepository.findByInviteCode(any())).thenReturn(Optional.empty());
        when(groupRepository.save(any(Group.class))).thenReturn(saved);
        when(groupMemberRepository.save(any(GroupMember.class))).thenAnswer(invocation -> invocation.getArgument(0));

        GroupService service = new GroupService(groupRepository, groupMemberRepository, eventPublisher, "f1.group.events");
        Group result = service.createGroup("Champions Club", "admin-1", "admin");

        assertThat(result.getId()).isEqualTo(10);
        assertThat(result.getAdminUserId()).isEqualTo("admin-1");
        verify(groupMemberRepository, times(1)).save(any(GroupMember.class));
        verify(eventPublisher, times(1)).publish(eq("f1.group.events"), eq("10"), any());
    }

    @Test
    void shouldRejectJoiningWhenAlreadyMember() {
        when(groupMemberRepository.existsByGroupIdAndUserId(4, "user-1")).thenReturn(true);

        GroupService service = new GroupService(groupRepository, groupMemberRepository, eventPublisher, "f1.group.events");

        assertThatThrownBy(() -> service.joinGroup(4, "user-1"))
            .isInstanceOf(IllegalStateException.class)
            .hasMessageContaining("already a member");
    }

    @Test
    void shouldRejectAdminLeavingGroup() {
        Group group = new Group();
        group.setId(2);
        group.setAdminUserId("admin-2");

        when(groupRepository.findById(2)).thenReturn(Optional.of(group));

        GroupService service = new GroupService(groupRepository, groupMemberRepository, eventPublisher, "f1.group.events");

        assertThatThrownBy(() -> service.leaveGroup(2, "admin-2"))
            .isInstanceOf(IllegalStateException.class)
            .hasMessageContaining("Admin cannot leave");
    }

    @Test
    void shouldRenameGroupWhenRequesterIsAdmin() {
        Group group = new Group();
        group.setId(6);
        group.setName("Old");
        group.setAdminUserId("admin-6");

        when(groupRepository.findById(6)).thenReturn(Optional.of(group));
        when(groupRepository.save(any(Group.class))).thenAnswer(invocation -> invocation.getArgument(0));

        GroupService service = new GroupService(groupRepository, groupMemberRepository, eventPublisher, "f1.group.events");
        Group renamed = service.renameGroup(6, "admin-6", "New Name");

        assertThat(renamed.getName()).isEqualTo("New Name");
    }

    @Test
    void shouldRejectNonAdminRename() {
        Group group = new Group();
        group.setId(7);
        group.setAdminUserId("admin-7");

        when(groupRepository.findById(7)).thenReturn(Optional.of(group));

        GroupService service = new GroupService(groupRepository, groupMemberRepository, eventPublisher, "f1.group.events");

        assertThatThrownBy(() -> service.renameGroup(7, "user-7", "Name"))
            .isInstanceOf(SecurityException.class)
            .hasMessageContaining("Only admin");
    }

    @Test
    void shouldDeleteGroupWhenRequesterIsAdmin() {
        Group group = new Group();
        group.setId(8);
        group.setAdminUserId("admin-8");

        when(groupRepository.findById(8)).thenReturn(Optional.of(group));

        GroupService service = new GroupService(groupRepository, groupMemberRepository, eventPublisher, "f1.group.events");
        service.deleteGroup(8, "admin-8");

        verify(groupMemberRepository, times(1)).deleteByGroupId(8);
        verify(groupRepository, times(1)).delete(group);
    }

    @Test
    void shouldRejectSystemUnlock() {
        Group group = new Group();
        group.setId(9);
        group.setAdminUserId("admin-9");
        group.setLockMode("system");
        group.setPredictionsLocked(true);

        when(groupRepository.findById(9)).thenReturn(Optional.of(group));

        GroupService service = new GroupService(groupRepository, groupMemberRepository, eventPublisher, "f1.group.events");

        assertThatThrownBy(() -> service.unlockPredictions(9, "admin-9"))
            .isInstanceOf(IllegalStateException.class)
            .hasMessageContaining("Cannot manually unlock");
    }
}

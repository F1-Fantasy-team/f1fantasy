package no.f1fantasy.repository;

import no.f1fantasy.entity.Group;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface GroupRepository extends JpaRepository<Group, Integer> {
    Optional<Group> findByInviteCode(String inviteCode);

    /** All groups a user is a member of (via GroupMembers join). */
    List<Group> findByMembers_UserId(String userId);

    /** True when the specified user is admin of the group. */
    boolean existsByIdAndAdminUserId(Integer id, String adminUserId);

    /** All groups that have a given lockMode and are not yet locked. */
    List<Group> findByLockModeInAndPredictionsLockedFalse(List<String> lockModes);
}

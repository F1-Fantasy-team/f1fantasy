package no.f1fantasy.repository;

import no.f1fantasy.entity.GroupMember;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface GroupMemberRepository extends JpaRepository<GroupMember, Integer> {
    List<GroupMember> findByGroupId(Integer groupId);
    Optional<GroupMember> findByGroupIdAndUserId(Integer groupId, String userId);
    boolean existsByGroupIdAndUserId(Integer groupId, String userId);
    void deleteByGroupIdAndUserId(Integer groupId, String userId);
    void deleteByGroupId(Integer groupId);
    int countByGroupId(Integer groupId);
}

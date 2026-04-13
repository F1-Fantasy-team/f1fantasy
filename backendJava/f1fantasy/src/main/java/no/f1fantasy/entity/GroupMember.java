package no.f1fantasy.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.Data;
import lombok.EqualsAndHashCode;
import lombok.ToString;

import java.time.OffsetDateTime;

@Entity
@Table(name = "GroupMembers",
        indexes = {
                @Index(name = "IX_GroupMembers_GroupId_UserId", columnList = "GroupId, UserId", unique = true),
                @Index(name = "IX_GroupMembers_UserId", columnList = "UserId")
        })
@Data
public class GroupMember {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "Id")
    private Integer id;

    @Column(name = "GroupId", nullable = false)
    private Integer groupId;

    @Column(name = "UserId", length = 100, nullable = false)
    private String userId;

    @Column(name = "JoinedAt", nullable = false)
    private OffsetDateTime joinedAt;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "GroupId", insertable = false, updatable = false)
    @JsonIgnore
    @ToString.Exclude
    @EqualsAndHashCode.Exclude
    private Group group;
}

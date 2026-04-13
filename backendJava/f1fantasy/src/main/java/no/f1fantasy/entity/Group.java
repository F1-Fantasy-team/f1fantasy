package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "Groups",
        indexes = {
                @Index(name = "IX_Groups_InviteCode", columnList = "InviteCode", unique = true),
                @Index(name = "IX_Groups_AdminUserId", columnList = "AdminUserId")
        })
@Data
public class Group {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "Id")
    private Integer id;

    @Column(name = "Name", length = 200, nullable = false)
    private String name;

    @Column(name = "InviteCode", length = 50, nullable = false)
    private String inviteCode;

    /** Values: "admin", "system", "hybrid" */
    @Column(name = "LockMode", length = 20, nullable = false)
    private String lockMode;

    @Column(name = "AdminUserId", length = 100, nullable = false)
    private String adminUserId;

    @Column(name = "CreatedAt", nullable = false)
    private OffsetDateTime createdAt;

    @Column(name = "PredictionsLocked", nullable = false)
    private boolean predictionsLocked;

    @Column(name = "LockedAt")
    private OffsetDateTime lockedAt;

    @OneToMany(mappedBy = "group", cascade = CascadeType.ALL, orphanRemoval = true)
    private List<GroupMember> members = new ArrayList<>();
}

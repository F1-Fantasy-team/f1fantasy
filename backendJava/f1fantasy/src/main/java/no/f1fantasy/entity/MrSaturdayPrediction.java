package no.f1fantasy.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.Data;
import lombok.EqualsAndHashCode;
import lombok.ToString;

import java.time.OffsetDateTime;

@Entity
@Table(name = "MrSaturdayPredictions",
        indexes = {
                @Index(name = "IX_MrSaturdayPredictions_GroupId_UserId", columnList = "GroupId, UserId", unique = true),
                @Index(name = "IX_MrSaturdayPredictions_UserId", columnList = "UserId")
        })
@Data
public class MrSaturdayPrediction {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "Id")
    private Integer id;

    @Column(name = "UserId", length = 100, nullable = false)
    private String userId;

    @Column(name = "GroupId", nullable = false)
    private Integer groupId;

    @Column(name = "Driver1Id", length = 100)
    private String driver1Id;

    @Column(name = "Driver2Id", length = 100)
    private String driver2Id;

    @Column(name = "CreatedAt", nullable = false)
    private OffsetDateTime createdAt;

    @Column(name = "UpdatedAt")
    private OffsetDateTime updatedAt;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "GroupId", insertable = false, updatable = false)
    @JsonIgnore
    @ToString.Exclude
    @EqualsAndHashCode.Exclude
    private Group group;
}

package no.f1fantasy.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.Data;
import lombok.EqualsAndHashCode;
import lombok.ToString;

import java.time.OffsetDateTime;

@Entity
@Table(name = "WildcardPredictions",
        indexes = {
                @Index(name = "IX_WildcardPredictions_GroupId_UserId", columnList = "GroupId, UserId", unique = true),
                @Index(name = "IX_WildcardPredictions_UserId", columnList = "UserId")
        })
@Data
public class WildcardPrediction {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "Id")
    private Integer id;

    @Column(name = "UserId", length = 100, nullable = false)
    private String userId;

    @Column(name = "GroupId", nullable = false)
    private Integer groupId;

    @Column(name = "Statement", length = 500)
    private String statement;

    /** Set by admin — value between 100 and 200. */
    @Column(name = "PointsPotential")
    private Integer pointsPotential;

    /** Note: column name matches the .NET typo "Fullfilled". */
    @Column(name = "Fullfilled")
    private Boolean fullfilled;

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

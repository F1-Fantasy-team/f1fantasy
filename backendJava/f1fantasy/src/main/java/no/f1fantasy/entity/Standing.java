package no.f1fantasy.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.Data;
import lombok.EqualsAndHashCode;
import lombok.ToString;

import java.time.OffsetDateTime;

@Entity
@Table(name = "Standings",
        indexes = {
                @Index(name = "IX_Standings_GroupId_UserId", columnList = "GroupId, UserId", unique = true),
                @Index(name = "IX_Standings_GroupId_Rank", columnList = "GroupId, Rank"),
                @Index(name = "IX_Standings_UserId", columnList = "UserId")
        })
@Data
public class Standing {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "Id")
    private Integer id;

    @Column(name = "UserId", length = 100, nullable = false)
    private String userId;

    @Column(name = "GroupId", nullable = false)
    private Integer groupId;

    @Column(name = "TotalScore", nullable = false)
    private int totalScore;

    @Column(name = "Rank", nullable = false)
    private int rank;

    /** Per-category scores serialised as JSON, e.g. {"driverChampionship":10,"driverDraft":5,...} */
    @Column(name = "CategoryScoresJson")
    private String categoryScoresJson;

    @Column(name = "UpdatedAt", nullable = false)
    private OffsetDateTime updatedAt;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "GroupId", insertable = false, updatable = false)
    @JsonIgnore
    @ToString.Exclude
    @EqualsAndHashCode.Exclude
    private Group group;
}

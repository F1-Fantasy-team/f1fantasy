package no.f1fantasy.entity;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.Data;
import lombok.EqualsAndHashCode;
import lombok.ToString;
import no.f1fantasy.entity.converter.StringListJsonConverter;

import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "DriverChampionshipPredictions",
        indexes = {
                @Index(name = "IX_DriverChampionshipPredictions_GroupId_UserId", columnList = "GroupId, UserId", unique = true),
                @Index(name = "IX_DriverChampionshipPredictions_UserId", columnList = "UserId")
        })
@Data
public class DriverChampionshipPrediction {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "Id")
    private Integer id;

    @Column(name = "UserId", length = 100, nullable = false)
    private String userId;

    @Column(name = "GroupId", nullable = false)
    private Integer groupId;

    @Convert(converter = StringListJsonConverter.class)
    @Column(name = "RankedDriverIds", nullable = false)
    private List<String> rankedDriverIds = new ArrayList<>();

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

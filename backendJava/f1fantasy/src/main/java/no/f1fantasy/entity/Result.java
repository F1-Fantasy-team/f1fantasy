package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

@Entity
@Table(name = "Results",
        indexes = {
                @Index(name = "IX_Results_Season_Round", columnList = "Season, Round"),
                @Index(name = "IX_Results_DriverId", columnList = "DriverId"),
                @Index(name = "IX_Results_ConstructorId", columnList = "ConstructorId"),
                @Index(name = "IX_Results_StatusId", columnList = "StatusId")
        })
@Data
public class Result {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "Id")
    private Integer id;

    @Column(name = "Season", length = 10, nullable = false)
    private String season;

    @Column(name = "Round", length = 10, nullable = false)
    private String round;

    @Column(name = "Number", length = 10)
    private String number;

    @Column(name = "Position", length = 10)
    private String position;

    @Column(name = "PositionText", length = 10)
    private String positionText;

    @Column(name = "Points", length = 10)
    private String points;

    @Column(name = "DriverId", length = 100, nullable = false)
    private String driverId;

    @Column(name = "ConstructorId", length = 100, nullable = false)
    private String constructorId;

    /** Nullable — sprint races or races with incomplete data may have null grid position. */
    @Column(name = "Grid", length = 10)
    private String grid;

    @Column(name = "Laps", length = 10)
    private String laps;

    @Column(name = "Status", length = 100)
    private String status;

    @Column(name = "StatusId", length = 10)
    private String statusId;

    @Column(name = "IsSprint", nullable = false)
    private boolean isSprint;

    @Embedded
    @AttributeOverrides({
            @AttributeOverride(name = "millis", column = @Column(name = "Time_Millis", length = 50)),
            @AttributeOverride(name = "time", column = @Column(name = "Time_Time", length = 50))
    })
    private ResultTime resultTime;

    @Embedded
    @AttributeOverrides({
            @AttributeOverride(name = "rank", column = @Column(name = "FastestLap_Rank", length = 10)),
            @AttributeOverride(name = "lap", column = @Column(name = "FastestLap_Lap", length = 10)),
            // Nested via FastestLap embeddable's own @AttributeOverrides
            @AttributeOverride(name = "lapTime.time", column = @Column(name = "FastestLap_Time_Time", length = 50)),
            @AttributeOverride(name = "averageSpeed.units", column = @Column(name = "FastestLap_AverageSpeed_Units", length = 10)),
            @AttributeOverride(name = "averageSpeed.speed", column = @Column(name = "FastestLap_AverageSpeed_Speed", length = 20))
    })
    private FastestLap fastestLap;
}

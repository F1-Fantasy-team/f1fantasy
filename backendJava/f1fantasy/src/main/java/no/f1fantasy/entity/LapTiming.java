package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

@Entity
@Table(name = "LapTimings",
        indexes = {
                @Index(name = "IX_LapTimings_Season_Round", columnList = "Season, Round"),
                @Index(name = "IX_LapTimings_Season_Round_LapNumber", columnList = "Season, Round, LapNumber"),
                @Index(name = "IX_LapTimings_Season_Round_DriverId", columnList = "Season, Round, DriverId"),
                @Index(name = "IX_LapTimings_DriverId", columnList = "DriverId")
        })
@Data
public class LapTiming {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "Id")
    private Integer id;

    @Column(name = "Season", length = 10, nullable = false)
    private String season;

    @Column(name = "Round", length = 10, nullable = false)
    private String round;

    @Column(name = "LapNumber", length = 10, nullable = false)
    private String lapNumber;

    @Column(name = "DriverId", length = 100, nullable = false)
    private String driverId;

    @Column(name = "Position", length = 10, nullable = false)
    private String position;

    @Column(name = "Time", length = 20)
    private String time;
}

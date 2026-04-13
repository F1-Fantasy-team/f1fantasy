package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

@Entity
@Table(name = "PitStops",
        indexes = {
                @Index(name = "IX_PitStops_Season_Round", columnList = "Season, Round"),
                @Index(name = "IX_PitStops_Season_Round_DriverId", columnList = "Season, Round, DriverId"),
                @Index(name = "IX_PitStops_DriverId", columnList = "DriverId")
        })
@Data
public class PitStop {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "Id")
    private Integer id;

    @Column(name = "Season", length = 10, nullable = false)
    private String season;

    @Column(name = "Round", length = 10, nullable = false)
    private String round;

    @Column(name = "DriverId", length = 100, nullable = false)
    private String driverId;

    @Column(name = "Lap", length = 10, nullable = false)
    private String lap;

    @Column(name = "Stop", length = 10, nullable = false)
    private String stop;

    @Column(name = "Time", length = 20)
    private String time;

    @Column(name = "Duration", length = 20)
    private String duration;
}

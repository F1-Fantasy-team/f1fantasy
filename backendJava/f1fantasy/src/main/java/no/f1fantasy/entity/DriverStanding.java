package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

@Entity
@Table(name = "DriverStandings",
        indexes = {
                @Index(name = "IX_DriverStandings_Season", columnList = "Season"),
                @Index(name = "IX_DriverStandings_DriverId", columnList = "DriverId")
        })
@IdClass(DriverStandingId.class)
@Data
public class DriverStanding {

    @Id
    @Column(name = "Season", length = 10)
    private String season;

    @Id
    @Column(name = "DriverId", length = 100)
    private String driverId;

    @Column(name = "Round", length = 10, nullable = false)
    private String round;

    @Column(name = "Position", length = 10, nullable = false)
    private String position;

    @Column(name = "PositionText", length = 10, nullable = false)
    private String positionText;

    @Column(name = "Points", length = 10, nullable = false)
    private String points;

    @Column(name = "Wins", length = 10, nullable = false)
    private String wins;

    @Column(name = "ConstructorId", length = 100, nullable = false)
    private String constructorId;
}

package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

@Entity
@Table(name = "ConstructorStandings",
        indexes = {
                @Index(name = "IX_ConstructorStandings_Season", columnList = "Season"),
                @Index(name = "IX_ConstructorStandings_ConstructorId", columnList = "ConstructorId")
        })
@IdClass(ConstructorStandingId.class)
@Data
public class ConstructorStanding {

    @Id
    @Column(name = "Season", length = 10)
    private String season;

    @Id
    @Column(name = "ConstructorId", length = 100)
    private String constructorId;

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
}

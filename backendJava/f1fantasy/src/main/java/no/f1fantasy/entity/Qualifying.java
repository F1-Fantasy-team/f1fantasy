package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

@Entity
@Table(name = "Qualifyings",
        indexes = {
                @Index(name = "IX_Qualifyings_Season_Round", columnList = "Season, Round"),
                @Index(name = "IX_Qualifyings_DriverId", columnList = "DriverId"),
                @Index(name = "IX_Qualifyings_ConstructorId", columnList = "ConstructorId")
        })
@Data
public class Qualifying {

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

    @Column(name = "DriverId", length = 100, nullable = false)
    private String driverId;

    @Column(name = "ConstructorId", length = 100, nullable = false)
    private String constructorId;

    @Column(name = "Q1", length = 20)
    private String q1;

    @Column(name = "Q2", length = 20)
    private String q2;

    @Column(name = "Q3", length = 20)
    private String q3;
}

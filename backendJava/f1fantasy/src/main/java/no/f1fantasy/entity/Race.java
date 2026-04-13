package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

@Entity
@Table(name = "Races")
@IdClass(RaceId.class)
@Data
public class Race {

    @Id
    @Column(name = "Season", length = 10)
    private String season;

    @Id
    @Column(name = "Round", length = 10)
    private String round;

    @Column(name = "Url", length = 500)
    private String url;

    @Column(name = "RaceName", length = 200)
    private String raceName;

    @Column(name = "Date", length = 50)
    private String date;

    @Column(name = "Time", length = 50)
    private String time;

    // Circuit is NOT stored in the Races table (ignored by EF Core); returned from Ergast API only
    @Transient
    private Circuit circuit;

    @Embedded
    @AttributeOverrides({
            @AttributeOverride(name = "date", column = @Column(name = "FirstPractice_Date", length = 50)),
            @AttributeOverride(name = "time", column = @Column(name = "FirstPractice_Time", length = 50))
    })
    private Session firstPractice;

    @Embedded
    @AttributeOverrides({
            @AttributeOverride(name = "date", column = @Column(name = "SecondPractice_Date", length = 50)),
            @AttributeOverride(name = "time", column = @Column(name = "SecondPractice_Time", length = 50))
    })
    private Session secondPractice;

    @Embedded
    @AttributeOverrides({
            @AttributeOverride(name = "date", column = @Column(name = "ThirdPractice_Date", length = 50)),
            @AttributeOverride(name = "time", column = @Column(name = "ThirdPractice_Time", length = 50))
    })
    private Session thirdPractice;

    @Embedded
    @AttributeOverrides({
            @AttributeOverride(name = "date", column = @Column(name = "Qualifying_Date", length = 50)),
            @AttributeOverride(name = "time", column = @Column(name = "Qualifying_Time", length = 50))
    })
    private Session qualifying;

    @Embedded
    @AttributeOverrides({
            @AttributeOverride(name = "date", column = @Column(name = "Sprint_Date", length = 50)),
            @AttributeOverride(name = "time", column = @Column(name = "Sprint_Time", length = 50))
    })
    private Session sprint;

    @Embedded
    @AttributeOverrides({
            @AttributeOverride(name = "date", column = @Column(name = "SprintQualifying_Date", length = 50)),
            @AttributeOverride(name = "time", column = @Column(name = "SprintQualifying_Time", length = 50))
    })
    private Session sprintQualifying;
}

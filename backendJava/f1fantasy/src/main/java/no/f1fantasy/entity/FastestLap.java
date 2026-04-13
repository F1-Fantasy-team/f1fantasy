package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

/** Embeddable for fastest lap data. Column names set via {@code @AttributeOverrides} on {@link Result}. */
@Embeddable
@Data
public class FastestLap {

    private String rank;
    private String lap;

    /** Nested: maps to FastestLap_Time_Time column. */
    @Embedded
    @AttributeOverrides({
            @AttributeOverride(name = "time", column = @Column(name = "FastestLap_Time_Time", length = 50))
    })
    private LapTime lapTime;

    /** Nested: maps to FastestLap_AverageSpeed_Units and FastestLap_AverageSpeed_Speed columns. */
    @Embedded
    @AttributeOverrides({
            @AttributeOverride(name = "units", column = @Column(name = "FastestLap_AverageSpeed_Units", length = 10)),
            @AttributeOverride(name = "speed", column = @Column(name = "FastestLap_AverageSpeed_Speed", length = 20))
    })
    private AverageSpeed averageSpeed;
}

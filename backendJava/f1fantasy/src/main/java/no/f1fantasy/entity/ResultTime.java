package no.f1fantasy.entity;

import jakarta.persistence.Embeddable;
import lombok.Data;

/** Embeddable for race result finish time. Column names set via {@code @AttributeOverrides}. */
@Embeddable
@Data
public class ResultTime {
    private String millis;
    private String time;
}

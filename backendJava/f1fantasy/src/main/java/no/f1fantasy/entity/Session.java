package no.f1fantasy.entity;

import jakarta.persistence.Embeddable;
import lombok.Data;

/**
 * Embeddable for session date/time pairs (FP1, FP2, FP3, Qualifying, Sprint, SprintQualifying).
 * Column names are provided via {@code @AttributeOverrides} on the owning {@link Race} entity.
 */
@Embeddable
@Data
public class Session {
    private String date;
    private String time;
}

package no.f1fantasy.entity;

import jakarta.persistence.Embeddable;
import lombok.Data;

@Embeddable
@Data
public class AverageSpeed {
    private String units;
    private String speed;
}

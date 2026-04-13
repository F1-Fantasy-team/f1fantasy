package no.f1fantasy.entity;

import jakarta.persistence.Embeddable;
import lombok.Data;

@Embeddable
@Data
public class LapTime {
    private String time;
}

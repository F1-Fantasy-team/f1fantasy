package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

@Entity
@Table(name = "Circuits")
@Data
public class Circuit {

    @Id
    @Column(name = "CircuitId", length = 100)
    private String circuitId;

    @Column(name = "Url", length = 500)
    private String url;

    @Column(name = "CircuitName", length = 200)
    private String circuitName;

    @Embedded
    private Location location;
}

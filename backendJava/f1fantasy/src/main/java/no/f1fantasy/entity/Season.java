package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

@Entity
@Table(name = "Seasons")
@Data
public class Season {

    @Id
    @Column(name = "Year", length = 10)
    private String year;

    @Column(name = "Url", length = 500)
    private String url;
}

package no.f1fantasy.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Embeddable;
import lombok.Data;

@Embeddable
@Data
public class Location {

    @Column(name = "Location_Lat", length = 50)
    private String lat;

    @Column(name = "Location_Long", length = 50)
    private String long_;

    @Column(name = "Location_Locality", length = 200)
    private String locality;

    @Column(name = "Location_Country", length = 200)
    private String country;
}

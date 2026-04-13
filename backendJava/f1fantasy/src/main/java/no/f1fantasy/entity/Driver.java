package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;
import no.f1fantasy.entity.converter.StringArrayConverter;

import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "Drivers")
@Data
public class Driver {

    @Id
    @Column(name = "DriverId", length = 100)
    private String driverId;

    @Column(name = "PermanentNumber", length = 10)
    private String permanentNumber;

    @Column(name = "Code", length = 10)
    private String code;

    @Column(name = "Url", length = 500)
    private String url;

    @Column(name = "GivenName", length = 100)
    private String givenName;

    @Column(name = "FamilyName", length = 100)
    private String familyName;

    @Column(name = "DateOfBirth", length = 50)
    private String dateOfBirth;

    @Column(name = "Nationality", length = 100)
    private String nationality;

    /** Stored as {@code text[]} in PostgreSQL, as a JSON-like array string in H2. */
    @Convert(converter = StringArrayConverter.class)
    @Column(name = "ActiveSeasons")
    private List<String> activeSeasons = new ArrayList<>();
}

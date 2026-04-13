package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;
import no.f1fantasy.entity.converter.StringArrayConverter;

import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "Constructors")
@Data
public class Constructor {

    @Id
    @Column(name = "ConstructorId", length = 100)
    private String constructorId;

    @Column(name = "Url", length = 500)
    private String url;

    @Column(name = "Name", length = 200)
    private String name;

    @Column(name = "Nationality", length = 100)
    private String nationality;

    /** Stored as {@code text[]} in PostgreSQL. */
    @Convert(converter = StringArrayConverter.class)
    @Column(name = "ActiveSeasons")
    private List<String> activeSeasons = new ArrayList<>();
}

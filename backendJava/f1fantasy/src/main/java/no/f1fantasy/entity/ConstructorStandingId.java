package no.f1fantasy.entity;

import lombok.Data;
import lombok.EqualsAndHashCode;

import java.io.Serializable;

@Data
@EqualsAndHashCode
public class ConstructorStandingId implements Serializable {
    private String season;
    private String constructorId;
}

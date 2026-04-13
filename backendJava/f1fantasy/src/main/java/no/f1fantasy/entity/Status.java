package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

@Entity
@Table(name = "Statuses",
        indexes = {
                @Index(name = "IX_Statuses_StatusText", columnList = "StatusText")
        })
@Data
public class Status {

    @Id
    @Column(name = "StatusId", length = 10)
    private String statusId;

    @Column(name = "StatusText", length = 100, nullable = false)
    private String statusText;

    @Column(name = "Count", length = 10, nullable = false)
    private String count;
}

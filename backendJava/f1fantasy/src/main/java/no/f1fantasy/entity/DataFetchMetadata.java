package no.f1fantasy.entity;

import jakarta.persistence.*;
import lombok.Data;

import java.time.OffsetDateTime;

@Entity
@Table(name = "DataFetchMetadata",
        indexes = {
                @Index(name = "IX_DataFetchMetadata_Season_DataType", columnList = "Season, DataType", unique = true)
        })
@Data
public class DataFetchMetadata {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "Id")
    private Integer id;

    @Column(name = "Season", length = 10, nullable = false)
    private String season;

    /** E.g. "Races", "Results", "Standings", "Qualifying", etc. */
    @Column(name = "DataType", length = 50, nullable = false)
    private String dataType;

    @Column(name = "LastFetchedAt", nullable = false)
    private OffsetDateTime lastFetchedAt;

    @Column(name = "LatestRoundAtFetch")
    private Integer latestRoundAtFetch;

    @Column(name = "FetchSuccessful", nullable = false)
    private boolean fetchSuccessful;

    @Column(name = "ErrorMessage", length = 500)
    private String errorMessage;
}

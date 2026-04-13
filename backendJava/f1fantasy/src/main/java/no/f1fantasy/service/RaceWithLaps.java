package no.f1fantasy.service;

import lombok.Data;
import no.f1fantasy.entity.LapTiming;

import java.util.List;

@Data
public class RaceWithLaps {
    private String season;
    private String round;
    private List<Lap> laps;

    @Data
    public static class Lap {
        private String number;
        private List<LapTiming> timings;
    }
}

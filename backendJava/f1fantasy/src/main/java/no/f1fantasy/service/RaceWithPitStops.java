package no.f1fantasy.service;

import no.f1fantasy.entity.PitStop;

import java.util.ArrayList;
import java.util.List;

public class RaceWithPitStops {

    private String season;
    private String round;
    private List<PitStop> pitStops = new ArrayList<>();

    public String getSeason() {
        return season;
    }

    public void setSeason(String season) {
        this.season = season;
    }

    public String getRound() {
        return round;
    }

    public void setRound(String round) {
        this.round = round;
    }

    public List<PitStop> getPitStops() {
        return pitStops;
    }

    public void setPitStops(List<PitStop> pitStops) {
        this.pitStops = pitStops;
    }
}

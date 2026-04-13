package no.f1fantasy.service;

import no.f1fantasy.entity.Qualifying;

import java.util.ArrayList;
import java.util.List;

public class RaceWithQualifying {

    private String season;
    private String round;
    private List<Qualifying> qualifyingResults = new ArrayList<>();

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

    public List<Qualifying> getQualifyingResults() {
        return qualifyingResults;
    }

    public void setQualifyingResults(List<Qualifying> qualifyingResults) {
        this.qualifyingResults = qualifyingResults;
    }
}

package no.f1fantasy.service;

import no.f1fantasy.entity.Result;

import java.util.ArrayList;
import java.util.List;

public class RaceWithResults {

    private String season;
    private String round;
    private List<Result> results = new ArrayList<>();
    private List<Result> sprintResults = new ArrayList<>();

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

    public List<Result> getResults() {
        return results;
    }

    public void setResults(List<Result> results) {
        this.results = results;
    }

    public List<Result> getSprintResults() {
        return sprintResults;
    }

    public void setSprintResults(List<Result> sprintResults) {
        this.sprintResults = sprintResults;
    }
}

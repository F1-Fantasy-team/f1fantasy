package no.f1fantasy.service;

import java.time.Duration;

public class CacheStalenessOptions {

    public static final CacheStalenessOptions DEFAULT = new CacheStalenessOptions();

    private Duration currentSeasonExpiration = Duration.ofHours(1);
    private Duration pastSeasonExpiration = Duration.ofDays(7);
    private boolean checkRaceSchedule = true;
    private Duration raceDataAvailabilityBuffer = Duration.ofDays(1);

    public Duration getCurrentSeasonExpiration() {
        return currentSeasonExpiration;
    }

    public void setCurrentSeasonExpiration(Duration currentSeasonExpiration) {
        this.currentSeasonExpiration = currentSeasonExpiration;
    }

    public Duration getPastSeasonExpiration() {
        return pastSeasonExpiration;
    }

    public void setPastSeasonExpiration(Duration pastSeasonExpiration) {
        this.pastSeasonExpiration = pastSeasonExpiration;
    }

    public boolean isCheckRaceSchedule() {
        return checkRaceSchedule;
    }

    public void setCheckRaceSchedule(boolean checkRaceSchedule) {
        this.checkRaceSchedule = checkRaceSchedule;
    }

    public Duration getRaceDataAvailabilityBuffer() {
        return raceDataAvailabilityBuffer;
    }

    public void setRaceDataAvailabilityBuffer(Duration raceDataAvailabilityBuffer) {
        this.raceDataAvailabilityBuffer = raceDataAvailabilityBuffer;
    }

    public static CacheStalenessOptions forQualifying() {
        CacheStalenessOptions options = new CacheStalenessOptions();
        options.setRaceDataAvailabilityBuffer(Duration.ZERO);
        return options;
    }

    public static CacheStalenessOptions forResults() {
        return new CacheStalenessOptions();
    }

    public static CacheStalenessOptions forStandings() {
        return new CacheStalenessOptions();
    }
}

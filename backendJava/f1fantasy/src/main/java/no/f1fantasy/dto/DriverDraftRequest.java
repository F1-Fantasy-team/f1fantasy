package no.f1fantasy.dto;

public class DriverDraftRequest {
    private String driver1Id;
    private String driver2Id;

    public DriverDraftRequest() {
    }

    public DriverDraftRequest(String driver1Id, String driver2Id) {
        this.driver1Id = driver1Id;
        this.driver2Id = driver2Id;
    }

    public String getDriver1Id() {
        return driver1Id;
    }

    public void setDriver1Id(String driver1Id) {
        this.driver1Id = driver1Id;
    }

    public String getDriver2Id() {
        return driver2Id;
    }

    public void setDriver2Id(String driver2Id) {
        this.driver2Id = driver2Id;
    }
}

package no.f1fantasy.dto;

public class CreateGroupRequest {
    private String name;
    private String lockMode;

    public CreateGroupRequest() {
    }

    public CreateGroupRequest(String name, String lockMode) {
        this.name = name;
        this.lockMode = lockMode;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getLockMode() {
        return lockMode;
    }

    public void setLockMode(String lockMode) {
        this.lockMode = lockMode;
    }
}

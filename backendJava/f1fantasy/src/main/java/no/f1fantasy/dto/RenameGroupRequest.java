package no.f1fantasy.dto;

public class RenameGroupRequest {
    private String newName;

    public RenameGroupRequest() {
    }

    public RenameGroupRequest(String newName) {
        this.newName = newName;
    }

    public String getNewName() {
        return newName;
    }

    public void setNewName(String newName) {
        this.newName = newName;
    }
}

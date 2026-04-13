package no.f1fantasy.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

import java.util.ArrayList;
import java.util.List;

@ConfigurationProperties(prefix = "clerk")
public class ClerkProperties {

    private String secretKey;
    private List<String> issuers = new ArrayList<>();
    private int jwksCacheHours = 24;

    public String getSecretKey() {
        return secretKey;
    }

    public void setSecretKey(String secretKey) {
        this.secretKey = secretKey;
    }

    public List<String> getIssuers() {
        return issuers;
    }

    public void setIssuers(List<String> issuers) {
        this.issuers = issuers;
    }

    public int getJwksCacheHours() {
        return jwksCacheHours;
    }

    public void setJwksCacheHours(int jwksCacheHours) {
        this.jwksCacheHours = jwksCacheHours;
    }
}

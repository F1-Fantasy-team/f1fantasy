package no.f1fantasy;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.ConfigurationPropertiesScan;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.scheduling.annotation.EnableScheduling;

@SpringBootApplication
@ConfigurationPropertiesScan
@EnableCaching
@EnableScheduling
public class F1FantasyApplication {

    public static void main(String[] args) {
        validateRequiredEnvVars();
        SpringApplication.run(F1FantasyApplication.class, args);
    }

    private static void validateRequiredEnvVars() {
        String clerkSecretKey = System.getenv("CLERK_SECRET_KEY");
        if (clerkSecretKey == null || clerkSecretKey.isBlank()) {
            throw new IllegalStateException(
                "Required environment variable CLERK_SECRET_KEY is not set. " +
                "Ensure it is defined in your .env file or environment."
            );
        }
    }
}

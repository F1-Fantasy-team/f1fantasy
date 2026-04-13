# ── Build stage ──────────────────────────────────────────────────────────
FROM maven:3.9.6-eclipse-temurin-21 AS build
WORKDIR /build

# Copy pom first to leverage layer caching for dependencies
COPY pom.xml .
RUN mvn dependency:go-offline -q

COPY src ./src
RUN mvn package -DskipTests -q

# ── Runtime stage ─────────────────────────────────────────────────────────
FROM eclipse-temurin:21-jre-jammy
WORKDIR /app

# Non-root user for security
RUN groupadd --system appgroup && useradd --system --gid appgroup appuser

COPY --from=build /build/target/f1fantasy-*.jar app.jar

RUN chown appuser:appgroup app.jar
USER appuser

# Spring Boot will bind to PORT env var via server.port=${PORT:8080}
EXPOSE 8080

ENTRYPOINT ["java", \
    "-XX:+UseContainerSupport", \
    "-XX:MaxRAMPercentage=75.0", \
    "-Djava.security.egd=file:/dev/./urandom", \
    "-jar", "app.jar"]

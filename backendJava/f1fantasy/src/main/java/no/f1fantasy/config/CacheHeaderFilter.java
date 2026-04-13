package no.f1fantasy.config;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import no.f1fantasy.entity.DataFetchMetadata;
import no.f1fantasy.repository.DataFetchMetadataRepository;
import org.springframework.http.HttpHeaders;
import org.springframework.core.annotation.Order;
import org.springframework.lang.NonNull;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.time.Duration;
import java.time.OffsetDateTime;
import java.util.Optional;

@Component
@Order(no.f1fantasy.config.FilterOrder.CACHE_HEADERS)
public class CacheHeaderFilter extends OncePerRequestFilter {

    private static final Duration STANDINGS_NO_CACHE_WINDOW = Duration.ofMinutes(2);
    private static final String DRIVER_STANDINGS_METADATA = "DriverStandings";
    private static final String CONSTRUCTOR_STANDINGS_METADATA = "ConstructorStandings";

    private final DataFetchMetadataRepository metadataRepository;

    public CacheHeaderFilter(DataFetchMetadataRepository metadataRepository) {
        this.metadataRepository = metadataRepository;
    }

    @Override
    protected boolean shouldNotFilter(@NonNull HttpServletRequest request) {
        return !"GET".equalsIgnoreCase(request.getMethod());
    }

    @Override
    protected void doFilterInternal(
        @NonNull HttpServletRequest request,
        @NonNull HttpServletResponse response,
        @NonNull FilterChain filterChain
    ) throws ServletException, IOException {

        String path = request.getRequestURI().toLowerCase();
        if (isRecentlyFetchedStandings(path, request)) {
            response.setHeader(HttpHeaders.CACHE_CONTROL, "no-cache, must-revalidate");
            response.setHeader("Pragma", "no-cache");
        } else if (isAggressivelyCachedPath(path)) {
            response.setHeader(HttpHeaders.CACHE_CONTROL, "public, max-age=3600");
        } else if (isMinimallyCachedPath(path)) {
            response.setHeader(HttpHeaders.CACHE_CONTROL, "public, max-age=30");
        } else {
            response.setHeader(HttpHeaders.CACHE_CONTROL, "no-cache, must-revalidate");
        }

        filterChain.doFilter(request, response);
    }

    private boolean isAggressivelyCachedPath(String path) {
        return path.contains("/driver")
            || path.contains("/constructor")
            || path.contains("/circuit")
            || path.contains("/season")
            || path.contains("/race")
            || path.contains("/qualifying")
            || path.contains("/result")
            || path.contains("/pitstop")
            || path.contains("/laptiming");
    }

    private boolean isMinimallyCachedPath(String path) {
        return path.contains("/standings") || path.contains("/predictions");
    }

    private boolean isRecentlyFetchedStandings(String path, HttpServletRequest request) {
        if (!path.contains("/standings")) {
            return false;
        }

        String season = request.getParameter("season");
        if (season == null || season.isBlank()) {
            return false;
        }

        try {
            return wasFetchedWithinWindow(season, DRIVER_STANDINGS_METADATA)
                || wasFetchedWithinWindow(season, CONSTRUCTOR_STANDINGS_METADATA);
        } catch (RuntimeException ex) {
            return true;
        }
    }

    private boolean wasFetchedWithinWindow(String season, String dataType) {
        Optional<DataFetchMetadata> metadataOpt = metadataRepository.findBySeasonAndDataType(season, dataType);
        if (metadataOpt.isEmpty()) {
            return false;
        }

        DataFetchMetadata metadata = metadataOpt.get();
        if (!metadata.isFetchSuccessful() || metadata.getLastFetchedAt() == null) {
            return false;
        }

        OffsetDateTime threshold = OffsetDateTime.now().minus(STANDINGS_NO_CACHE_WINDOW);
        return metadata.getLastFetchedAt().isAfter(threshold);
    }
}

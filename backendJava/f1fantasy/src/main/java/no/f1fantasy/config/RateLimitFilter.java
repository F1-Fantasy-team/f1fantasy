package no.f1fantasy.config;

import com.fasterxml.jackson.databind.ObjectMapper;
import io.github.bucket4j.Bandwidth;
import io.github.bucket4j.Bucket;
import io.github.bucket4j.ConsumptionProbe;
import no.f1fantasy.dto.ErrorResponse;
import no.f1fantasy.service.RateLimitViolationMonitor;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.http.MediaType;
import org.springframework.lang.NonNull;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.time.Duration;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Component
public class RateLimitFilter extends OncePerRequestFilter {

    private final RateLimitProperties properties;
    private final RateLimitViolationMonitor violationMonitor;
    private final ObjectMapper objectMapper;
    private final Map<String, Bucket> buckets = new ConcurrentHashMap<>();

    public RateLimitFilter(
        RateLimitProperties properties,
        RateLimitViolationMonitor violationMonitor,
        ObjectMapper objectMapper
    ) {
        this.properties = properties;
        this.violationMonitor = violationMonitor;
        this.objectMapper = objectMapper;
    }

    @Override
    protected boolean shouldNotFilter(@NonNull HttpServletRequest request) {
        String path = request.getRequestURI();
        return path.startsWith("/actuator") || path.startsWith("/swagger-ui") || path.startsWith("/api-docs");
    }

    @Override
    protected void doFilterInternal(
        @NonNull HttpServletRequest request,
        @NonNull HttpServletResponse response,
        @NonNull FilterChain filterChain
    )
        throws ServletException, IOException {

        RateLimitProperties.Policy policy = resolvePolicy(request);
        String key = buildKey(request);
        Bucket bucket = buckets.computeIfAbsent(key, ignored -> createBucket(policy));

        ConsumptionProbe probe = bucket.tryConsumeAndReturnRemaining(1);
        if (probe.isConsumed()) {
            response.setHeader("X-RateLimit-Remaining", String.valueOf(probe.getRemainingTokens()));
            filterChain.doFilter(request, response);
            return;
        }

        violationMonitor.recordViolation(request.getRemoteAddr());

        long retryAfter = Math.max(1, probe.getNanosToWaitForRefill() / 1_000_000_000L);
        response.setStatus(429);
        response.setHeader("Retry-After", String.valueOf(retryAfter));
        response.setContentType(MediaType.APPLICATION_JSON_VALUE);
        objectMapper.writeValue(response.getWriter(), new ErrorResponse("Rate limit exceeded"));
    }

    private RateLimitProperties.Policy resolvePolicy(HttpServletRequest request) {
        String path = request.getRequestURI();
        if (path.startsWith("/api/admin")) {
            return properties.getAdmin();
        }

        String method = request.getMethod();
        if ("GET".equalsIgnoreCase(method)) {
            return properties.getRead();
        }

        return properties.getWrite();
    }

    private String buildKey(HttpServletRequest request) {
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();
        if (authentication != null && authentication.isAuthenticated() && authentication.getName() != null) {
            return "user:" + authentication.getName();
        }

        String ip = request.getRemoteAddr();
        return ip == null ? "anonymous" : "ip:" + ip;
    }

    private Bucket createBucket(RateLimitProperties.Policy policy) {
        Bandwidth limit = Bandwidth.builder()
            .capacity(policy.getCapacity())
            .refillIntervally(policy.getRefillTokens(), Duration.ofSeconds(policy.getRefillPeriodSeconds()))
            .build();
        return Bucket.builder().addLimit(limit).build();
    }
}

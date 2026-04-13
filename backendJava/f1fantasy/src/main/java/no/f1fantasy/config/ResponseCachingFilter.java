package no.f1fantasy.config;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.core.annotation.Order;
import org.springframework.http.HttpHeaders;
import org.springframework.lang.NonNull;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;
import org.springframework.web.util.ContentCachingResponseWrapper;

import java.io.IOException;
import java.time.Duration;
import java.time.OffsetDateTime;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Component
@Order(no.f1fantasy.config.FilterOrder.RESPONSE_CACHING)
public class ResponseCachingFilter extends OncePerRequestFilter {

    private static final int MAX_ENTRIES = 1000;
    private static final int MAX_BODY_BYTES = 1024 * 1024;

    private static class CachedResponse {
        private final int status;
        private final String contentType;
        private final String cacheControl;
        private final byte[] body;
        private final OffsetDateTime expiresAt;

        private CachedResponse(int status, String contentType, String cacheControl, byte[] body, OffsetDateTime expiresAt) {
            this.status = status;
            this.contentType = contentType;
            this.cacheControl = cacheControl;
            this.body = body;
            this.expiresAt = expiresAt;
        }
    }

    private final Map<String, CachedResponse> cache = new ConcurrentHashMap<>();

    @Override
    protected boolean shouldNotFilter(@NonNull HttpServletRequest request) {
        if (!"GET".equalsIgnoreCase(request.getMethod())) {
            return true;
        }

        if (request.getHeader(HttpHeaders.AUTHORIZATION) != null) {
            return true;
        }

        String path = request.getRequestURI();
        return path.startsWith("/actuator") || path.startsWith("/swagger-ui") || path.startsWith("/api-docs");
    }

    @Override
    protected void doFilterInternal(
        @NonNull HttpServletRequest request,
        @NonNull HttpServletResponse response,
        @NonNull FilterChain filterChain
    ) throws ServletException, IOException {

        String cacheKey = buildCacheKey(request);
        CachedResponse cached = cache.get(cacheKey);
        OffsetDateTime now = OffsetDateTime.now();

        if (cached != null && cached.expiresAt.isAfter(now)) {
            response.setStatus(cached.status);
            if (cached.contentType != null) {
                response.setContentType(cached.contentType);
            }
            if (cached.cacheControl != null) {
                response.setHeader(HttpHeaders.CACHE_CONTROL, cached.cacheControl);
            }
            response.setHeader("X-Response-Cache", "HIT");
            response.getOutputStream().write(cached.body);
            return;
        }

        if (cached != null) {
            cache.remove(cacheKey);
        }

        ContentCachingResponseWrapper wrapped = new ContentCachingResponseWrapper(response);
        filterChain.doFilter(request, wrapped);

        String path = request.getRequestURI().toLowerCase();
        Duration ttl = resolveTtl(path);
        String cacheControl = resolveCacheControl(path);
        if (!ttl.isZero() && wrapped.getStatus() == HttpServletResponse.SC_OK) {
            byte[] body = wrapped.getContentAsByteArray();
            if (body.length > 0 && body.length <= MAX_BODY_BYTES) {
                evictIfNeeded(now);
                if (cache.size() < MAX_ENTRIES) {
                    cache.put(cacheKey, new CachedResponse(
                        wrapped.getStatus(),
                        wrapped.getContentType(),
                        cacheControl,
                        body.clone(),
                        now.plus(ttl)
                    ));
                }
            }
            wrapped.setHeader("X-Response-Cache", "MISS");
        }

        wrapped.copyBodyToResponse();
    }

    private void evictIfNeeded(OffsetDateTime now) {
        if (cache.size() < MAX_ENTRIES) {
            return;
        }

        cache.entrySet().removeIf(entry -> entry.getValue().expiresAt.isBefore(now));
    }

    private String buildCacheKey(HttpServletRequest request) {
        String query = request.getQueryString();
        if (query == null || query.isBlank()) {
            return request.getMethod() + "|" + request.getRequestURI();
        }
        return request.getMethod() + "|" + request.getRequestURI() + "?" + query;
    }

    private Duration resolveTtl(String path) {
        if (isAggressivelyCachedPath(path)) {
            return Duration.ofHours(1);
        }
        if (isMinimallyCachedPath(path)) {
            return Duration.ofSeconds(30);
        }
        return Duration.ZERO;
    }

    private String resolveCacheControl(String path) {
        if (isAggressivelyCachedPath(path)) {
            return "public, max-age=3600";
        }
        if (isMinimallyCachedPath(path)) {
            return "public, max-age=30";
        }
        return "no-cache, must-revalidate";
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
}

package no.f1fantasy.config;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.slf4j.MDC;
import org.springframework.core.annotation.Order;
import org.springframework.lang.NonNull;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.UUID;

@Component
@Order(no.f1fantasy.config.FilterOrder.REQUEST_CONTEXT)
@SuppressWarnings("hiding")
public class RequestContextLoggingFilter extends OncePerRequestFilter {

    private static final Logger appLogger = LoggerFactory.getLogger(RequestContextLoggingFilter.class);
    private static final String REQUEST_ID_HEADER = "X-Request-Id";
    private static final String REQUEST_ID_MDC_KEY = "requestId";

    @Override
    protected void doFilterInternal(
        @NonNull HttpServletRequest request,
        @NonNull HttpServletResponse response,
        @NonNull FilterChain filterChain
    ) throws ServletException, IOException {

        String requestId = UUID.randomUUID().toString();
        long startedAt = System.currentTimeMillis();
        response.setHeader(REQUEST_ID_HEADER, requestId);
        MDC.put(REQUEST_ID_MDC_KEY, requestId);

        appLogger.info("Request started: {} {}", request.getMethod(), request.getRequestURI());
        try {
            filterChain.doFilter(request, response);
            long elapsed = System.currentTimeMillis() - startedAt;
            appLogger.info("Request completed: {} {} status={} durationMs={}",
                request.getMethod(), request.getRequestURI(), response.getStatus(), elapsed);
        } finally {
            MDC.remove(REQUEST_ID_MDC_KEY);
        }
    }
}

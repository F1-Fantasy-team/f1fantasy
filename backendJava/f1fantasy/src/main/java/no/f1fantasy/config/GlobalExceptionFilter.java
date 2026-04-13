package no.f1fantasy.config;

import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import no.f1fantasy.dto.ErrorResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.core.annotation.Order;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.lang.NonNull;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.NoSuchElementException;

@Component
@Order(no.f1fantasy.config.FilterOrder.GLOBAL_EXCEPTION)
public class GlobalExceptionFilter extends OncePerRequestFilter {

    private static final Logger logger = LoggerFactory.getLogger(GlobalExceptionFilter.class);

    private final ObjectMapper objectMapper;

    public GlobalExceptionFilter(ObjectMapper mapper) {
        this.objectMapper = mapper;
    }

    @Override
    protected void doFilterInternal(
        @NonNull HttpServletRequest request,
        @NonNull HttpServletResponse response,
        @NonNull FilterChain filterChain
    ) throws ServletException, IOException {

        try {
            filterChain.doFilter(request, response);
        } catch (RuntimeException | ServletException | IOException ex) {
            if (response.isCommitted()) {
                throw ex;
            }

            HttpStatus status = mapStatus(ex);
            logger.error("Unhandled request exception for {} {}", request.getMethod(), request.getRequestURI(), ex);

            response.resetBuffer();
            response.setStatus(status.value());
            response.setContentType(MediaType.APPLICATION_JSON_VALUE);
            objectMapper.writeValue(response.getWriter(), new ErrorResponse(mapMessage(status, ex)));
            response.flushBuffer();
        }
    }

    private HttpStatus mapStatus(Exception ex) {
        if (ex instanceof IllegalArgumentException) {
            return HttpStatus.BAD_REQUEST;
        }
        if (ex instanceof SecurityException || ex instanceof AccessDeniedException) {
            return HttpStatus.FORBIDDEN;
        }
        if (ex instanceof NoSuchElementException) {
            return HttpStatus.NOT_FOUND;
        }
        if (ex instanceof DataIntegrityViolationException) {
            return HttpStatus.CONFLICT;
        }
        return HttpStatus.INTERNAL_SERVER_ERROR;
    }

    private String mapMessage(HttpStatus status, Exception ex) {
        if (status == HttpStatus.INTERNAL_SERVER_ERROR) {
            return "An internal server error occurred";
        }
        String message = ex.getMessage();
        return (message == null || message.isBlank()) ? status.getReasonPhrase() : message;
    }
}

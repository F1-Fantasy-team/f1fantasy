package no.f1fantasy.config;

import com.fasterxml.jackson.databind.ObjectMapper;
import no.f1fantasy.dto.ErrorResponse;
import no.f1fantasy.service.IpBlacklistService;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.http.MediaType;
import org.springframework.lang.NonNull;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;

@Component
public class IpBlacklistFilter extends OncePerRequestFilter {

    private final IpBlacklistService blacklistService;
    private final ObjectMapper objectMapper;

    public IpBlacklistFilter(IpBlacklistService blacklistService, ObjectMapper objectMapper) {
        this.blacklistService = blacklistService;
        this.objectMapper = objectMapper;
    }

    @Override
    protected void doFilterInternal(
        @NonNull HttpServletRequest request,
        @NonNull HttpServletResponse response,
        @NonNull FilterChain filterChain
    )
        throws ServletException, IOException {

        String ipAddress = request.getRemoteAddr();
        if (ipAddress != null && blacklistService.isBlacklisted(ipAddress)) {
            response.setStatus(HttpServletResponse.SC_FORBIDDEN);
            response.setContentType(MediaType.APPLICATION_JSON_VALUE);
            objectMapper.writeValue(response.getWriter(), new ErrorResponse("Access denied"));
            return;
        }

        filterChain.doFilter(request, response);
    }
}

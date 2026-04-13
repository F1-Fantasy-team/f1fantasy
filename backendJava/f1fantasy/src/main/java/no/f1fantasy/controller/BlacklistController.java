package no.f1fantasy.controller;

import java.time.Duration;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import no.f1fantasy.service.IpBlacklistService;

@RestController
@RequestMapping("/api/admin/blacklist")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class BlacklistController {

    private final IpBlacklistService blacklistService;

    public BlacklistController(IpBlacklistService blacklistService) {
        this.blacklistService = blacklistService;
    }

    @GetMapping
    public ResponseEntity<?> getBlacklistedIps() {
        try {
            Map<String, IpBlacklistService.BlacklistEntry> blacklist = blacklistService.getBlacklistedIps();
            List<String> blacklistedIps = blacklist.keySet().stream().collect(Collectors.toList());
            return ResponseEntity.ok(blacklistedIps);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error fetching blacklist"));
        }
    }

    @PostMapping
    public ResponseEntity<?> blacklistIp(@RequestBody BlacklistRequest request) {
        try {
            if (request.getIpAddress() == null || request.getIpAddress().isEmpty()) {
                return ResponseEntity.status(400).body(errorMap("IP address is required"));
            }

            if (request.getDurationMinutes() != null && request.getDurationMinutes() > 0) {
                Duration duration = Duration.ofMinutes(request.getDurationMinutes());
                blacklistService.blacklist(request.getIpAddress(), request.getReason() != null ? request.getReason() : "Manual blacklist", duration);
            } else {
                blacklistService.blacklist(request.getIpAddress(), request.getReason() != null ? request.getReason() : "Manual blacklist");
            }

            Map<String, String> response = new HashMap<>();
            response.put("message", "IP " + request.getIpAddress() + " has been blacklisted");
            return ResponseEntity.ok(response);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error blacklisting IP: " + ex.getMessage()));
        }
    }

    @DeleteMapping("/{ipAddress}")
    public ResponseEntity<?> unblacklistIp(@PathVariable String ipAddress) {
        try {
            blacklistService.unblacklist(ipAddress);

            Map<String, String> response = new HashMap<>();
            response.put("message", "IP " + ipAddress + " has been removed from blacklist");
            return ResponseEntity.ok(response);
        } catch (Exception ex) {
            return ResponseEntity.status(500).body(errorMap("Error removing IP from blacklist: " + ex.getMessage()));
        }
    }

    private Map<String, String> errorMap(String message) {
        Map<String, String> error = new HashMap<>();
        error.put("error", message);
        return error;
    }

    public static class BlacklistRequest {
        private String ipAddress;
        private String reason;
        private Integer durationMinutes;

        public String getIpAddress() {
            return ipAddress;
        }

        public void setIpAddress(String ipAddress) {
            this.ipAddress = ipAddress;
        }

        public String getReason() {
            return reason;
        }

        public void setReason(String reason) {
            this.reason = reason;
        }

        public Integer getDurationMinutes() {
            return durationMinutes;
        }

        public void setDurationMinutes(Integer durationMinutes) {
            this.durationMinutes = durationMinutes;
        }
    }
}

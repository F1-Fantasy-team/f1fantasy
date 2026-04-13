package no.f1fantasy.service;

import org.springframework.stereotype.Component;

import java.time.OffsetDateTime;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;

@Component
public class PaginationStateTracker {

    private final ConcurrentMap<String, PaginationState> states = new ConcurrentHashMap<>();

    public PaginationState getState(String endpoint) {
        return states.computeIfAbsent(endpoint, key -> new PaginationState());
    }

    public void updateState(String endpoint, int offset, int total, int limit) {
        PaginationState state = states.computeIfAbsent(endpoint, key -> new PaginationState());
        state.setLastSuccessfulOffset(offset);
        state.setTotal(total);
        state.setLastUpdate(OffsetDateTime.now());
        state.setComplete(offset + limit >= total);
    }

    public void markComplete(String endpoint) {
        PaginationState state = states.computeIfAbsent(endpoint, key -> new PaginationState());
        state.setComplete(true);
        state.setLastUpdate(OffsetDateTime.now());
    }

    public void reset(String endpoint) {
        states.remove(endpoint);
    }

    public boolean shouldFetch(String endpoint) {
        PaginationState state = states.get(endpoint);
        if (state == null) {
            return true;
        }

        if (!state.isComplete()) {
            return true;
        }

        return state.getLastUpdate().isBefore(OffsetDateTime.now().minusHours(1));
    }

    public int getNextOffset(String endpoint, int limit) {
        PaginationState state = states.get(endpoint);
        if (state == null || state.isComplete()) {
            return 0;
        }
        return state.getLastSuccessfulOffset() + limit;
    }

    public static class PaginationState {
        private int lastSuccessfulOffset;
        private int total;
        private boolean complete;
        private OffsetDateTime lastUpdate = OffsetDateTime.now();

        public int getLastSuccessfulOffset() {
            return lastSuccessfulOffset;
        }

        public void setLastSuccessfulOffset(int lastSuccessfulOffset) {
            this.lastSuccessfulOffset = lastSuccessfulOffset;
        }

        public int getTotal() {
            return total;
        }

        public void setTotal(int total) {
            this.total = total;
        }

        public boolean isComplete() {
            return complete;
        }

        public void setComplete(boolean complete) {
            this.complete = complete;
        }

        public OffsetDateTime getLastUpdate() {
            return lastUpdate;
        }

        public void setLastUpdate(OffsetDateTime lastUpdate) {
            this.lastUpdate = lastUpdate;
        }
    }
}

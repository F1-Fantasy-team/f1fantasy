package no.f1fantasy.service;

import org.junit.jupiter.api.Test;

import java.time.OffsetDateTime;

import static org.assertj.core.api.Assertions.assertThat;

class PaginationStateTrackerTest {

    @Test
    void shouldReturnDefaultStateWhenEndpointIsNew() {
        PaginationStateTracker tracker = new PaginationStateTracker();

        PaginationStateTracker.PaginationState state = tracker.getState("races-2025");

        assertThat(state.getLastSuccessfulOffset()).isZero();
        assertThat(state.getTotal()).isZero();
        assertThat(state.isComplete()).isFalse();
    }

    @Test
    void shouldMarkCompleteWhenOffsetReachesTotal() {
        PaginationStateTracker tracker = new PaginationStateTracker();

        tracker.updateState("races-2025", 60, 90, 30);

        assertThat(tracker.getState("races-2025").isComplete()).isTrue();
        assertThat(tracker.getNextOffset("races-2025", 30)).isZero();
    }

    @Test
    void shouldContinueFromLastOffsetWhenNotComplete() {
        PaginationStateTracker tracker = new PaginationStateTracker();

        tracker.updateState("races-2025", 30, 120, 30);

        assertThat(tracker.getState("races-2025").isComplete()).isFalse();
        assertThat(tracker.getNextOffset("races-2025", 30)).isEqualTo(60);
    }

    @Test
    void shouldFetchWhenStateIsStale() {
        PaginationStateTracker tracker = new PaginationStateTracker();
        PaginationStateTracker.PaginationState state = tracker.getState("races-2025");
        state.setComplete(true);
        state.setLastUpdate(OffsetDateTime.now().minusHours(2));

        assertThat(tracker.shouldFetch("races-2025")).isTrue();
    }

    @Test
    void shouldResetEndpointState() {
        PaginationStateTracker tracker = new PaginationStateTracker();
        tracker.updateState("races-2025", 30, 120, 30);

        tracker.reset("races-2025");

        assertThat(tracker.getNextOffset("races-2025", 30)).isZero();
        assertThat(tracker.shouldFetch("races-2025")).isTrue();
    }
}

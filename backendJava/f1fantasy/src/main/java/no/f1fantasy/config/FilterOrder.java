package no.f1fantasy.config;

import org.springframework.core.Ordered;

public final class FilterOrder {

    private FilterOrder() {
    }

    public static final int REQUEST_CONTEXT = Ordered.HIGHEST_PRECEDENCE;
    public static final int GLOBAL_EXCEPTION = REQUEST_CONTEXT + 10;
    public static final int RESPONSE_CACHING = REQUEST_CONTEXT + 20;
    public static final int CACHE_HEADERS = REQUEST_CONTEXT + 30;
}

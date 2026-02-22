# Rate Limiting & IP Blacklisting

This API implements comprehensive rate limiting and IP blacklisting to protect against abuse while maintaining a good user experience for legitimate users.

## Rate Limiting Policies

### Global Limit (Per IP)
- **Limit**: 200 requests per minute
- **Applied to**: All endpoints by default
- **Partitioned by**: IP address
- **Use case**: Prevents a single IP from overwhelming the server

### Read Policy (`read`)
- **Limit**: 100 requests per minute
- **Applied to**: GET endpoints (viewing data)
- **Partitioned by**: User ID (authenticated) or IP (anonymous)
- **Endpoints**:
  - GET /api/standings
  - GET /api/groups
  - GET /api/predictions
  - Most F1 data endpoints (drivers, races, standings, etc.)

### Write Policy (`write`)
- **Limit**: 20 requests per minute
- **Applied to**: POST/PUT/DELETE endpoints (modifying data)
- **Partitioned by**: User ID (authenticated) or IP (anonymous)
- **Endpoints**:
  - POST /api/predictions
  - PUT /api/predictions
  - POST /api/groups
  - PUT /api/groups

### Admin Policy (`admin`)
- **Limit**: 10 requests per minute
- **Applied to**: Admin-only endpoints
- **Partitioned by**: User ID
- **Endpoints**:
  - POST /api/admin/*
  - DELETE /api/admin/*
  - Blacklist management

## How to Apply Rate Limiting to Controllers

### Using Attributes

```csharp
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/standings")]
[EnableRateLimiting("read")] // Apply to entire controller
public class StandingsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStandings()
    {
        // Limited to 100 requests/minute per user
    }

    [HttpPost]
    [EnableRateLimiting("write")] // Override for specific endpoint
    public async Task<IActionResult> RecalculateStandings()
    {
        // Limited to 20 requests/minute per user
    }
}
```

### Disable Rate Limiting

```csharp
[DisableRateLimiting] // Exempt this endpoint
[HttpGet("health")]
public IActionResult HealthCheck()
{
    return Ok("healthy");
}
```

## IP Blacklisting

### Automatic Blacklisting
The system automatically blacklists IPs that:
- Exceed rate limits **10 times** within **5 minutes**
- Auto-ban duration: **1 hour**

### Manual Blacklisting (Admin)

#### View Blacklisted IPs
```http
GET /api/admin/blacklist
Authorization: Bearer {token}
```

**Response:**
```json
{
  "192.168.1.100": {
    "ipAddress": "192.168.1.100",
    "reason": "Auto-blacklisted: 10 rate limit violations in 5 minutes",
    "blacklistedAt": "2026-02-22T10:30:00Z",
    "expiresAt": "2026-02-22T11:30:00Z"
  }
}
```

#### Blacklist an IP
```http
POST /api/admin/blacklist
Authorization: Bearer {token}
Content-Type: application/json

{
  "ipAddress": "192.168.1.100",
  "reason": "Suspicious activity detected",
  "durationMinutes": 60  // Optional: null = permanent
}
```

#### Remove from Blacklist
```http
DELETE /api/admin/blacklist/192.168.1.100
Authorization: Bearer {token}
```

## Rate Limit Response

When a rate limit is exceeded, the API returns:

**Status**: `429 Too Many Requests`

**Headers**:
```
Retry-After: 60
```

**Body**:
```json
{
  "error": "Rate limit exceeded",
  "message": "Too many requests. Please slow down and try again later.",
  "retryAfter": "60 seconds"
}
```

## Blacklist Response

When accessing from a blacklisted IP:

**Status**: `403 Forbidden`

**Body**:
```json
{
  "error": "Access denied",
  "message": "Your IP address has been blocked due to suspicious activity. Contact support if you believe this is an error."
}
```

## BFF API Considerations

Since this is a Backend-for-Frontend API:

1. **Authenticated Users**: Rate limits partition by `userId` (from JWT), preventing one user from affecting others behind the same NAT/proxy
2. **Anonymous Traffic**: Falls back to IP-based limiting
3. **Reasonable Limits**: 
   - 100 reads/min handles normal browsing (1.6 requests/sec)
   - 20 writes/min prevents prediction spam
   - Multiple users behind same IP won't conflict (user-based partitioning)

## Monitoring

Check logs for rate limit violations:
```
[Warning] Rate limit exceeded for IP: 192.168.1.100, Endpoint: /api/predictions
[Warning] IP 192.168.1.100 exceeded rate limit 10 times in 5 minutes. Auto-blacklisting.
```

## Recommendations

### For Production:
1. **Adjust limits** based on actual traffic patterns after monitoring
2. **Add admin roles** to BlacklistController (currently any authenticated user can access)
3. **Persist blacklist** to database for multi-instance deployments
4. **Add metrics** to track rate limit hits and blacklist events
5. **Configure Redis** for distributed rate limiting if running multiple instances

### Typical User Behavior:
- **Viewing standings**: 1-5 requests/minute (well under 100 limit)
- **Submitting predictions**: 1-2 requests/minute (well under 20 limit)
- **Browsing F1 data**: 10-20 requests/minute during heavy use (under 100 limit)

### Attack Scenarios Protected:
- ✅ **DDoS attempts**: Global IP limit blocks flooding
- ✅ **Credential stuffing**: Write limits prevent brute force
- ✅ **Data scraping**: Read limits + auto-blacklisting slow scrapers
- ✅ **Spam predictions**: Write limits cap submissions

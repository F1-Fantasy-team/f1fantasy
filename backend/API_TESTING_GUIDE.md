# F1 Fantasy League API - Complete Reference

## Server Info
- **Authentication**: Clerk JWT Bearer token required for all endpoints
- **Authorization Header**: `Authorization: Bearer YOUR_CLERK_JWT_TOKEN`
- **Content-Type**: `application/json` for all POST requests

## Quick Test Flow

### 1. Create a Group
```http
POST /api/groups
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "name": "My F1 Fantasy League",
  "lockMode": "admin"
}
```

Response:
```json
{
  "id": 1,
  "name": "My F1 Fantasy League",
  "inviteCode": "ABC12XYZ",
  "lockMode": "admin",
  "adminUserId": "user_xxx",
  "predictionsLocked": false,
  "lockedAt": null,
  "createdAt": "2025-02-22T14:00:00Z",
  "members": [
    {
      "id": 1,
      "groupId": 1,
      "userId": "user_xxx",
      "displayName": "John Doe",
      "isAdmin": true,
      "joinedAt": "2025-02-22T14:00:00Z"
    }
  ]
}
```

**Note**: `displayName` is fetched from Clerk (name or username fallback). Admin is auto-added as first member.

### 2. Get My Groups
```http
GET /api/groups
Authorization: Bearer YOUR_TOKEN
```

Response: Array of groups you're a member of, with enriched member data

```json
[
  {
    "id": 1,
    "name": "My F1 Fantasy League",
    "inviteCode": "ABC12XYZ",
    "lockMode": "admin",
    "adminUserId": "user_xxx",
    "predictionsLocked": false,
    "lockedAt": null,
    "createdAt": "2025-02-22T14:00:00Z",
    "members": [
      {
        "id": 1,
        "groupId": 1,
        "userId": "user_xxx",
        "displayName": "John Doe",
        "isAdmin": true,
        "joinedAt": "2025-02-22T14:00:00Z"
      },
      {
        "id": 2,
        "groupId": 1,
        "userId": "user_yyy",
        "displayName": "Jane Smith",
        "isAdmin": false,
        "joinedAt": "2025-02-22T15:30:00Z"
      }
    ]
  }
]
```

### 3. Get Specific Group by ID
```http
GET /api/groups/1
Authorization: Bearer YOUR_TOKEN
```

Response: Group details with all members, their display names from Clerk, **and all their predictions**

```json
{
  "id": 1,
  "name": "My F1 Fantasy League",
  "inviteCode": "ABC12XYZ",
  "lockMode": "admin",
  "adminUserId": "user_xxx",
  "predictionsLocked": false,
  "lockedAt": null,
  "createdAt": "2025-02-22T14:00:00Z",
  "members": [
    {
      "id": 1,
      "groupId": 1,
      "userId": "user_xxx",
      "displayName": "John Doe",
      "isAdmin": true,
      "joinedAt": "2025-02-22T14:00:00Z",
      "driverChampionship": {
        "id": 1,
        "groupId": 1,
        "userId": "user_xxx",
        "rankedDriverIds": ["max_verstappen", "charles_leclerc", ...],
        "createdAt": "2025-02-22T10:00:00Z"
      },
      "constructorChampionship": { ... },
      "driverDraft": { ... },
      "destructor": { ... },
      "mrSaturday": { ... },
      "zeroPointer": { ... },
      "wildcard": { ... }
    },
    {
      "id": 2,
      "groupId": 1,
      "userId": "user_yyy",
      "displayName": "Jane Smith",
      "isAdmin": false,
      "joinedAt": "2025-02-22T15:30:00Z",
      "driverChampionship": null,
      "constructorChampionship": null,
      "driverDraft": { ... },
      "destructor": null,
      "mrSaturday": null,
      "zeroPointer": null,
      "wildcard": { ... }
    }
  ]
}
```

**Note**: Any prediction category that hasn't been submitted will be `null`. Perfect for displaying everyone's predictions in one call!

### 4. Get Group by Invite Code
```http
GET /api/groups/invite/ABC12XYZ
Authorization: Bearer YOUR_TOKEN
```

Response: Group details with members, display names, **and all predictions** (useful for preview before joining)

### 5. Join a Group
```http
POST /api/groups/1/join
Authorization: Bearer YOUR_TOKEN
```

### 6. Leave a Group
```http
POST /api/groups/1/leave
Authorization: Bearer YOUR_TOKEN
```

### 7. Rename a Group (Admin Only)
```http
PUT /api/groups/1
Authorization: Bearer ADMIN_TOKEN
Content-Type: application/json

{
  "name": "New Group Name"
}
```

### 8. Remove a Member (Admin Only)
```http
DELETE /api/groups/1/members/user_xyz
Authorization: Bearer ADMIN_TOKEN
```

### 9. Delete a Group (Admin Only)
```http
DELETE /api/groups/1
Authorization: Bearer ADMIN_TOKEN
```

### 10. Submit Driver Championship Prediction
```http
POST /api/predictions/groups/1/driver-championship
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "rankedDriverIds": [
    "max_verstappen",
    "lewis_hamilton",
    "charles_leclerc",
    "lando_norris",
    "oscar_piastri",
    "carlos_sainz",
    "george_russell",
    "fernando_alonso",
    "lance_stroll",
    "pierre_gasly",
    "esteban_ocon",
    "yuki_tsunoda",
    "liam_lawson",
    "alex_albon",
    "franco_colapinto",
    "nico_hulkenberg",
    "kevin_magnussen",
    "valtteri_bottas",
    "zhou_guanyu",
    "isack_hadjar"
  ]
}
```

### 3a. Get Driver Championship Prediction
```http
GET /api/predictions/groups/1/driver-championship
Authorization: Bearer YOUR_TOKEN
```

### 4. Submit Constructor Championship Prediction
```http
POST /api/predictions/groups/1/constructor-championship
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "rankedConstructorIds": [
    "red_bull",
    "mercedes",
    "ferrari",
    "mclaren",
    "aston_martin",
    "alpine",
    "racing_bulls",
    "williams",
    "haas",
    "stake"
  ]
}
```

### 4a. Get Constructor Championship Prediction
```http
GET /api/predictions/groups/1/constructor-championship
Authorization: Bearer YOUR_TOKEN
```

### 5. Submit Driver Draft (2 drivers for F1 points)
```http
POST /api/predictions/groups/1/driver-draft
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "driver1Id": "max_verstappen",
  "driver2Id": "lando_norris"
}
```

### 5a. Get Driver Draft Prediction
```http
GET /api/predictions/groups/1/driver-draft
Authorization: Bearer YOUR_TOKEN
```

### 6. Submit Destructor Prediction (2 drivers for DNFs)
```http
POST /api/predictions/groups/1/destructor
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "driver1Id": "zhou_guanyu",
  "driver2Id": "valtteri_bottas"
}
```

### 6a. Get Destructor Prediction
```http
GET /api/predictions/groups/1/destructor
Authorization: Bearer YOUR_TOKEN
```

### 7. Submit Mr Saturday Prediction (2 drivers for quali wins)
```http
POST /api/predictions/groups/1/mr-saturday
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "driver1Id": "max_verstappen",
  "driver2Id": "charles_leclerc"
}
```

### 7a. Get Mr Saturday Prediction
```http
GET /api/predictions/groups/1/mr-saturday
Authorization: Bearer YOUR_TOKEN
```

### 8. Submit Zero Pointer Prediction (unlimited drivers - penalty for wrong guesses)
```http
POST /api/predictions/groups/1/zero-pointer
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "driverIds": [
    "zhou_guanyu",
    "logan_sargeant",
    "valtteri_bottas"
  ]
}
```

**Note**: Can submit 0 to unlimited drivers. +100 points per correct prediction (driver scores 0), -20 penalty per incorrect prediction (driver has points).

### 8a. Get Zero Pointer Prediction
```http
GET /api/predictions/groups/1/zero-pointer
Authorization: Bearer YOUR_TOKEN
```

### 9. Submit Wildcard Prediction
```http
POST /api/predictions/groups/1/wildcard
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "statement": "Max Verstappen will win 20+ races this season"
}
```

### 9a. Get Wildcard Prediction
```http
GET /api/predictions/groups/1/wildcard
Authorization: Bearer YOUR_TOKEN
```

### 9b. Get All Wildcard Predictions in Group
```http
GET /api/predictions/groups/1/wildcards
Authorization: Bearer YOUR_TOKEN
```

**Note**: Returns all wildcard predictions for all members in the group. Useful for seeing what other people predicted.

### 10. Lock Predictions (Admin Only)
```http
POST /api/groups/1/lock
Authorization: Bearer ADMIN_TOKEN
```

### 11. Unlock Predictions (Admin Only)
```http
POST /api/groups/1/unlock
Authorization: Bearer ADMIN_TOKEN
```

### 13. Get Standings (Auto-Recalculates)
```http
GET /api/standings/groups/1?season=2025
Authorization: Bearer YOUR_TOKEN
```

**Note**: Standings automatically recalculate when new race results are available. No manual trigger needed!

Response:
```json
[
  {
    "id": 1,
    "userId": "user_xxx",
    "groupId": 1,
    "totalScore": 150,
    "rank": 1,
    "categoryScoresJson": "{\"DriverChampionship\":50,\"ConstructorChampionship\":30,\"DriverDraft\":40,\"Destructor\":20,\"MrSaturday\":10,\"ZeroPointer\":0,\"Wildcard\":0}",
    "updatedAt": "2025-02-22T14:30:00Z"
  }
]
```

## Common HTTP Status Codes

- **200 OK**: Success (GET requests)
- **201 Created**: Resource created (POST requests)
- **400 Bad Request**: Validation error (check response body for details)
- **401 Unauthorized**: Missing or invalid JWT token
- **403 Forbidden**: Not authorized (e.g., non-admin trying admin endpoint)
- **404 Not Found**: Resource doesn't exist

## Validation Rules

### Driver Championship Prediction
- Must include ALL active drivers (20-22 drivers)
- No duplicates allowed
- All driver IDs must exist in database
- Cannot submit if predictions are locked

### Constructor Championship Prediction
- Must include ALL active constructors (10 constructors)
- No duplicates allowed
- All constructor IDs must exist in database
- Cannot submit if predictions are locked

### Driver Draft
- Must select exactly 2 drivers
- No duplicates allowed
- Both drivers must exist in database
- Nullable fields allow partial submissions before lock

### Destructors / Mr Saturday
- Must select exactly 2 drivers
- No duplicates allowed
- Both drivers must exist in database
- Nullable fields allow partial submissions before lock

### Zero Pointers
- Can select 0 to unlimited drivers
- No duplicates allowed
- All driver IDs must exist in database
- Empty list allowed (scores 0 points)
- Scoring: +100 per correct (driver has 0 points), -20 per incorrect (driver has points)

### Wildcard
- Statement max 500 characters
- PointsPotential range: 100-200 (set by admin)
- Fullfilled boolean (set by admin)

## Lock Mode Behavior

### Admin Mode
- Predictions locked/unlocked manually by admin via API
- No automatic locking

### System Mode
- Predictions auto-lock when first race of season starts
- Background service checks every 5 minutes
- Cannot be manually unlocked once auto-locked

### Hybrid Mode
- Admin can manually lock early
- If not manually locked, auto-locks when first race starts
- Best of both worlds

## Testing Tips

1. **Get Clerk Token**: Use your frontend or Clerk Dashboard to get a valid JWT
2. **Test Lock States**: Try submitting predictions before/after locking
3. **Test Validation**: Try submitting invalid data (duplicates, wrong IDs, etc.)
4. **Test Admin Access**: Verify non-admins can't access admin endpoints
5. **Test Tie-Breaking**: Create multiple users with same score, check ranking
6. **Monitor Logs**: Check console for auto-lock service messages

## F1 Data Endpoints (Public Racing Data)

These endpoints provide access to historical F1 race data used for scoring predictions.

### Race Data
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/race/season/{season}` | Get all races for a season |
| GET | `/api/race/season/{season}/round/{round}` | Get specific race details |

### Results
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/result/season/{season}` | Get all race results for a season |
| GET | `/api/result/season/{season}/round/{round}` | Get results for a specific race |

### Qualifying
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/qualifying/season/{season}` | Get all qualifying results for a season |
| GET | `/api/qualifying/season/{season}/round/{round}` | Get qualifying for a specific race |

### Driver & Constructor Data
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/driver` | Get all drivers |
| GET | `/api/driver/{driverId}` | Get specific driver details |
| GET | `/api/constructor` | Get all constructors |
| GET | `/api/constructor/{constructorId}` | Get specific constructor details |

### Standings (F1 Official)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/driverstanding/season/{season}` | Get driver championship standings |
| GET | `/api/driverstanding/season/{season}/round/{round}` | Get standings after specific round |
| GET | `/api/constructorstanding/season/{season}` | Get constructor championship standings |
| GET | `/api/constructorstanding/season/{season}/round/{round}` | Get standings after specific round |

### Additional Data
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/circuit` | Get all circuits |
| GET | `/api/pitstop/season/{season}/round/{round}` | Get pit stops for a race |
| GET | `/api/laptiming/season/{season}/round/{round}` | Get lap times for a race |
| GET | `/api/status` | Get all status codes (finish/retirement reasons) |
| GET | `/api/season` | Get all available seasons |

**Example: Get 2025 Season Results**
```http
GET /api/result/season/2025
Authorization: Bearer YOUR_TOKEN
```

**Example: Get Drivers for Predictions**
```http
GET /api/driver
Authorization: Bearer YOUR_TOKEN
```

This returns all drivers with their IDs (e.g., `max_verstappen`) for use in predictions.

## Complete Endpoint Reference

### Fantasy League Endpoints

### Group Management Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/groups` | Create a new group | Required |
| GET | `/api/groups` | Get all groups for current user | Required |
| GET | `/api/groups/{id}` | Get specific group by ID | Required |
| GET | `/api/groups/invite/{inviteCode}` | Get group by invite code | Required |
| POST | `/api/groups/{id}/join` | Join a group | Required |
| POST | `/api/groups/{id}/leave` | Leave a group | Required |
| PUT | `/api/groups/{id}` | Rename a group (admin only) | Admin |
| DELETE | `/api/groups/{id}/members/{targetUserId}` | Remove a member from group (admin only) | Admin |
| DELETE | `/api/groups/{id}` | Delete a group (admin only) | Admin |
| POST | `/api/groups/{id}/lock` | Lock predictions (admin only) | Admin |
| POST | `/api/groups/{id}/unlock` | Unlock predictions (admin only) | Admin |

### Prediction Endpoints

All prediction endpoints require authentication. Replace `{groupId}` with the group ID.

#### Championship Predictions
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/predictions/groups/{groupId}/driver-championship` | Submit driver championship ranking |
| GET | `/api/predictions/groups/{groupId}/driver-championship` | Get your driver championship prediction |
| POST | `/api/predictions/groups/{groupId}/constructor-championship` | Submit constructor championship ranking |
| GET | `/api/predictions/groups/{groupId}/constructor-championship` | Get your constructor championship prediction |

#### Two-Driver Categories
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/predictions/groups/{groupId}/driver-draft` | Submit driver draft (2 drivers for points) |
| GET | `/api/predictions/groups/{groupId}/driver-draft` | Get your driver draft prediction |
| POST | `/api/predictions/groups/{groupId}/destructor` | Submit destructor prediction (2 drivers for DNFs) |
| GET | `/api/predictions/groups/{groupId}/destructor` | Get your destructor prediction |
| POST | `/api/predictions/groups/{groupId}/mr-saturday` | Submit Mr Saturday prediction (2 drivers for poles) |
| GET | `/api/predictions/groups/{groupId}/mr-saturday` | Get your Mr Saturday prediction |

#### Zero Pointer (Unlimited Drivers)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/predictions/groups/{groupId}/zero-pointer` | Submit zero pointer prediction (unlimited drivers) |
| GET | `/api/predictions/groups/{groupId}/zero-pointer` | Get your zero pointer prediction |

#### Wildcard
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/predictions/groups/{groupId}/wildcard` | Submit wildcard statement |
| GET | `/api/predictions/groups/{groupId}/wildcard` | Get your wildcard prediction |
| GET | `/api/predictions/groups/{groupId}/wildcards` | Get all wildcard predictions in the group |

### Standings Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/standings/groups/{groupId}` | Get standings (auto-recalculates if new races available) | Required |
| POST | `/api/standings/groups/{groupId}/recalculate` | Force recalculate standings (usually not needed) | Required |
| GET | `/api/standings/groups/{groupId}/detailed` | Get detailed standings with round-by-round breakdown | Required |
| GET | `/api/standings/groups/{groupId}/me/breakdown` | Get detailed breakdown for current user | Required |
| GET | `/api/standings/groups/{groupId}/user/{userId}/breakdown` | Get detailed breakdown for specific user | Required |

**Smart Auto-Recalculation:**
- GET standings automatically checks if new race results are available
- Compares latest round with results vs. last calculated round (derived from existing standings)
- Only recalculates if new data is available - no wasted processing!
- Frontend simply calls GET without worrying about manual triggers

**Query Parameters:**
- `season` (optional): Year to calculate standings for (default: current year)

**Detailed Breakdown Response Structure:**
```json
{
  "userId": "user_abc",
  "totalScore": 450,
  "rank": 1,
  "categoryTotals": {
    "DriverChampionship": 80,
    "ConstructorChampionship": 60,
    "DriverDraft": 150,
    "Destructor": 100,
    "MrSaturday": 40,
    "ZeroPointer": 0,
    "Wildcard": 20
  },
  "roundScores": [
    {
      "round": "1",
      "raceName": "Round 1",
      "categoryScores": { "DriverDraft": 25, "Destructor": 20, "MrSaturday": 10 },
      "cumulativeScore": 55
    }
  ]
}
```

### Admin Endpoints

All admin endpoints require the user to be the group admin (except populate-season which is system-level).

| Method | Endpoint | Description |
|--------|----------|-------------|
| PUT | `/api/admin/groups/{groupId}/wildcard/{userId}/points` | Set wildcard points (100-200) |
| PUT | `/api/admin/groups/{groupId}/wildcard/{userId}/fulfilled` | Mark wildcard as fulfilled |
| GET | `/api/admin/groups/{groupId}/wildcards` | Get all wildcards in a group |
| POST | `/api/admin/populate-season/{season}` | Populate driver/constructor data for a season |

#### Populate Season (System Admin)
```http
POST /api/admin/populate-season/2026
Authorization: Bearer YOUR_TOKEN
```

Response:
```json
{
  "message": "Successfully populated season 2026",
  "driversCount": 20,
  "constructorsCount": 10
}
```

**Purpose**: Fetches all drivers and constructors for the specified season from the Ergast API and adds the season to their `ActiveSeasons` list. This should be called at the start of each season (especially during preseason) to ensure active participants are tracked before race standings exist.

## Category Scoring Rules

**Per-Round Categories** (points accumulate each race):
- **Driver Draft**: Sum of F1 points earned by your 2 drivers in each race
- **Destructor**: 20 points per DNF for your selected drivers
- **Mr Saturday**: 10 points per pole position for your selected drivers

**Season-End Categories** (only appear in final round):
- **Driver Championship**: 10 points exact match, -2 per position delta for ALL drivers
- **Constructor Championship**: 10 points exact match, -2 per position delta for ALL constructors
- **Zero Pointers**: +100 per correct prediction (driver has 0 points), -20 per incorrect (driver has points)
- **Wildcard**: 100-200 points (admin sets amount and marks fulfilled)

## Response Formats

### Group Response (GroupDto)

All GET endpoints for groups return enriched `GroupDto` objects with member display names fetched from Clerk:

```json
{
  "id": 1,
  "name": "My F1 Fantasy League",
  "inviteCode": "ABC12XYZ",
  "lockMode": "admin",
  "adminUserId": "user_xxx",
  "createdAt": "2025-02-22T14:00:00Z",
  "predictionsLocked": false,
  "lockedAt": null,
  "members": [
    {
      "id": 1,
      "groupId": 1,
      "userId": "user_xxx",
      "displayName": "John Doe",
      "isAdmin": true,
      "joinedAt": "2025-02-22T14:00:00Z"
    }
  ]
}
```

**Member Fields:**
- `displayName`: First name + last name from Clerk, falls back to username, then user ID
- `isAdmin`: Boolean indicating if this member is the group admin
- All other fields are standard group/member data

**Performance Note:** Display names are fetched in parallel from Clerk API for efficiency. Falls back gracefully to user IDs if Clerk API is unavailable.

## Request Formats

### Create Group
```json
{
  "name": "My League",
  "lockMode": "admin"  // "admin", "system", or "hybrid"
}
```

### Rename Group
```json
{
  "name": "New Group Name"
}
```

### Driver Championship
```json
{
  "rankedDriverIds": ["driver1", "driver2", ..., "driver20"]
}
```

*Must include ALL active drivers (20-22 depending on season)*

### Constructor Championship
```json
{
  "rankedConstructorIds": ["constructor1", "constructor2", ..., "constructor10"]
}
```

*Must include ALL active constructors (exactly 10)*

### Two-Driver Categories (Driver Draft, Destructor, Mr Saturday)
```json
{
  "driver1Id": "max_verstappen",
  "driver2Id": "lando_norris"
}
```

*Both fields nullable for partial submissions before lock*

### Zero Pointer (Unlimited Drivers)
```json
{
  "driverIds": [
    "zhou_guanyu",
    "valtteri_bottas",
    "logan_sargeant"
  ]
}
```

*Array can be empty or contain any number of driver IDs. No duplicates allowed. Scoring: +100 per correct (0 points), -20 per incorrect (has points)*

### Wildcard
```json
{
  "statement": "Your prediction statement (max 500 chars)"
}
```

## Response Formats

### Basic Standings
```json
{
  "id": 1,
  "userId": "user_abc",
  "groupId": 1,
  "totalScore": 450,
  "rank": 1,
  "categoryScoresJson": "{...}",
  "updatedAt": "2025-12-31T23:59:59Z"
}
```

### Detailed Standings Response
```json
{
  "userId": "user_abc",
  "groupId": 1,
  "totalScore": 450,
  "rank": 1,
  "categoryTotals": {
    "DriverChampionship": 80,
    "ConstructorChampionship": 60,
    "DriverDraft": 150,
    "Destructor": 100,
    "MrSaturday": 40,
    "ZeroPointer": 0,
    "Wildcard": 20
  },
  "roundScores": [
    {
      "round": "1",
      "raceName": "Round 1",
      "date": "2025-03-15T00:00:00Z",
      "categoryScores": {
        "DriverDraft": 25,
        "Destructor": 20,
        "MrSaturday": 10,
        "DriverChampionship": 0,
        "ConstructorChampionship": 0,
        "ZeroPointer": 0,
        "Wildcard": 0
      },
      "cumulativeScore": 55
    }
  ]
}
```

## Error Responses

All endpoints return consistent error responses:

```json
{
  "error": "Descriptive error message"
}
```

### Common Error Scenarios

**400 Bad Request**
- Invalid data format
- Validation errors (duplicates, wrong count, etc.)
- Predictions locked (trying to edit after lock)

**401 Unauthorized**
- Missing JWT token
- Invalid/expired JWT token
- User ID not found in token

**403 Forbidden**
- Not group admin (trying admin-only operations)
- Not group member (trying member-only operations)

**404 Not Found**
- Group doesn't exist
- Prediction doesn't exist
- User doesn't exist

**409 Conflict**
- Already a group member (trying to join again)
- Not a group member (trying to leave when not member)


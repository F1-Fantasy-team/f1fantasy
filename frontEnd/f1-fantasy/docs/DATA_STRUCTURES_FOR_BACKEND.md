# Data structures to persist (for backend/DB)

Frontend will send/receive these shapes. All IDs are strings. Driver and constructor IDs match your API (e.g. lowercase: `ver`, `nor`, `rbr`, `fer`).

---

## 1. Group

What we need per group.

```ts
interface Group {
  id: string;           // UUID or slug
  name: string;
  memberCount: number;
  createdAt: string;   // ISO 8601, e.g. "2025-01-15T10:00:00Z"
  inviteCode?: string; // optional, e.g. "LEGEND24"
}
```

---

## 2. User predictions (per user, per group)

One row/document per **user + group**. `userId` is from auth (e.g. Clerk). `displayName` is for display only.

```ts
type PredictionCategoryId =
  | "driversChampionship"
  | "constructorsChampionship"
  | "driverDraft"
  | "destructors"
  | "mrSaturday"
  | "zeroPointers"
  | "wildcard";

interface UserPredictions {
  userId: string;
  displayName: string;

  // Optional: each category can be missing until user saves
  driversChampionship?: DriversChampionshipPrediction;
  constructorsChampionship?: ConstructorsChampionshipPrediction;
  driverDraft?: DriverDraftPrediction;
  destructors?: DestructorsPrediction;
  mrSaturday?: MrSaturdayPrediction;
  zeroPointers?: ZeroPointersPrediction;
  wildcard?: WildcardPrediction;
}
```

**Category payload types:**

| Category                 | Type / shape |
|--------------------------|-------------|
| driversChampionship      | `{ position: number; driverId: string }[]` — length = grid size (e.g. 20 or 22). position 1 = P1. |
| constructorsChampionship| `{ position: number; constructorId: string }[]` — length = 10. |
| driverDraft              | `{ driverId1: string; driverId2: string }` |
| destructors              | `{ driverId1: string; driverId2: string }` |
| mrSaturday               | `{ driverId1: string; driverId2: string }` |
| zeroPointers             | `{ driverIds: string[] }` — length 0 to grid size (e.g. 0–22). |
| wildcard                  | `{ statement: string }` — free text. |

**Examples:**

```json
{
  "userId": "user_abc123",
  "displayName": "Alex",
  "driversChampionship": [
    { "position": 1, "driverId": "ver" },
    { "position": 2, "driverId": "nor" }
  ],
  "constructorsChampionship": [
    { "position": 1, "constructorId": "rbr" },
    { "position": 2, "constructorId": "mcl" }
  ],
  "driverDraft": { "driverId1": "ver", "driverId2": "nor" },
  "destructors": { "driverId1": "mag", "driverId2": "str" },
  "mrSaturday": { "driverId1": "lec", "driverId2": "nor" },
  "zeroPointers": { "driverIds": ["zhou", "ric"] },
  "wildcard": { "statement": "Piastri wins a race" }
}
```

---

## 3. Standings (computed or stored)

We show per-group standings: overall score and per-category score. Backend can compute from predictions + your scoring rules, or store precomputed.

```ts
interface CategoryScore {
  categoryId: PredictionCategoryId;
  score: number;
}

interface MemberStanding {
  userId: string;
  displayName: string;
  overallScore: number;
  rank: number;
  categoryScores: CategoryScore[];
}
```

---

## 4. Lock state (per group)

Users can “lock” predictions (no more edits). We need to know who has locked.

```ts
// Per group: list of user IDs who have locked
lockedUserIds: string[]  // e.g. ["user_1", "user_2"]
```

Once season has started, frontend treats lock as irreversible (no unlock).

---

## 5. Full payload per group (for GET group predictions)

What the frontend expects when loading one group’s predictions page:

```ts
interface GroupPredictionsData {
  groupId: string;
  standings: MemberStanding[];
  predictions: UserPredictions[];
  lockedUserIds?: string[];
}
```

- **standings** — one entry per member, with `overallScore`, `rank`, `categoryScores` (one per category).
- **predictions** — one entry per member, with their raw prediction data (see §2).
- **lockedUserIds** — list of user IDs that have locked for this group.

---

## 6. IDs we use

- **Driver IDs:** from your API, e.g. `ver`, `nor`, `lec`, `ham` (lowercase).
- **Constructor IDs:** from your API, e.g. `rbr`, `fer`, `mcl` (lowercase).
- **userId:** from auth provider (e.g. Clerk `user.id`).
- **groupId:** your group primary key (UUID or slug).

---

## 7. Suggested DB usage (for backend)

- **Groups** — table/collection: `id`, `name`, `memberCount`, `createdAt`, `inviteCode`.
- **Group members** — table/collection: `groupId`, `userId`, `displayName`, `joinedAt` (so you know who is in which group and their display name).
- **Predictions** — table/collection keyed by `(groupId, userId)` with columns/fields for each category (or one JSON blob per `UserPredictions`).
- **Lock state** — either a `lockedUserIds` array on the group row, or a separate table `group_id`, `user_id` for locked users.
- **Standings** — either computed on read (from predictions + your scoring) or stored/cached per group and updated when predictions change or when results come in.

If you want, we can next define exact REST or GraphQL endpoints (e.g. `GET/PUT /groups/:id/predictions`, `POST /groups/:id/lock`) and request/response bodies to match these structures.

# 15 — Data Schema

> PostgreSQL 16. Truth lives here; Redis is only ever a cache or a queue.
> Migrations via EF Core. Hot read paths use Dapper for speed.

---

## 1. Conventions

| Rule | Detail |
|---|---|
| Primary keys | `bigint` identity, except `players.id` which is `uuid` |
| Timestamps | `bigint` **epoch milliseconds, UTC** — never `timestamptz` for game logic |
| Money/resources | `bigint` (integers only, no floats anywhere) |
| Soft delete | `deleted_at bigint NULL` on player-visible entities |
| Naming | `snake_case` tables and columns |
| Flexible blobs | `jsonb` for progression detail that changes shape often |
| Audit | Anything that grants currency writes an append-only audit row |

**Why epoch milliseconds instead of `timestamptz`:** the shared `Sim` takes time as a `long` parameter and must be identical on client and server. Storing what the sim uses removes a whole class of timezone and precision bugs.

---

## 2. Core tables

```sql
-- Accounts ------------------------------------------------------------
CREATE TABLE players (
  id              uuid PRIMARY KEY,
  device_id       text,
  email           text UNIQUE,
  auth_provider   text NOT NULL,            -- device | google | email
  display_name    text NOT NULL,
  age_declared    smallint,                 -- ★ voice gating (10 §6.1)
  created_at      bigint NOT NULL,
  last_login_at   bigint,
  banned_until    bigint,
  ban_reason      text,
  deleted_at      bigint
);
CREATE INDEX ix_players_device ON players(device_id);

-- One row per player per state (a player may exist in several states)
CREATE TABLE player_states (
  id              bigserial PRIMARY KEY,
  player_id       uuid NOT NULL REFERENCES players(id),
  state_id        int  NOT NULL,
  hq_level        int  NOT NULL DEFAULT 1,
  power           bigint NOT NULL DEFAULT 0,
  tile_x          int, tile_y int,
  alliance_id     bigint,
  vip_level       int NOT NULL DEFAULT 0,
  vip_points      bigint NOT NULL DEFAULT 0,
  shield_until    bigint,
  stamina         int NOT NULL DEFAULT 100,
  last_resolved_at bigint NOT NULL,          -- lazy evaluation anchor
  resources       jsonb NOT NULL,            -- {food, iron, coin, diamonds, valor}
  config_version  int NOT NULL,
  UNIQUE (player_id, state_id)
);
CREATE INDEX ix_ps_alliance ON player_states(alliance_id);
CREATE INDEX ix_ps_tile     ON player_states(state_id, tile_x, tile_y);
CREATE INDEX ix_ps_power    ON player_states(state_id, power DESC);

-- Base ----------------------------------------------------------------
CREATE TABLE buildings (
  id              bigserial PRIMARY KEY,
  player_state_id bigint NOT NULL REFERENCES player_states(id),
  building_def_id int  NOT NULL,
  level           int  NOT NULL DEFAULT 0,
  slot_index      int  NOT NULL,
  UNIQUE (player_state_id, slot_index)
);

CREATE TABLE jobs (                          -- builds, research, training, healing
  id              bigserial PRIMARY KEY,
  player_state_id bigint NOT NULL REFERENCES player_states(id),
  job_type        smallint NOT NULL,
  target_id       int NOT NULL,
  queue_index     int NOT NULL,
  started_at      bigint NOT NULL,
  completes_at    bigint NOT NULL,
  helps_received  int NOT NULL DEFAULT 0,    -- alliance help, max 30
  payload         jsonb
);
CREATE INDEX ix_jobs_completes ON jobs(completes_at);
CREATE INDEX ix_jobs_state     ON jobs(player_state_id);

CREATE TABLE troops (
  player_state_id bigint NOT NULL REFERENCES player_states(id),
  troop_type      smallint NOT NULL,         -- 0 tank, 1 air, 2 missile
  tier            smallint NOT NULL,         -- 1..8
  count_ready     bigint NOT NULL DEFAULT 0,
  count_wounded   bigint NOT NULL DEFAULT 0,
  PRIMARY KEY (player_state_id, troop_type, tier)
);

CREATE TABLE heroes (
  id              bigserial PRIMARY KEY,
  player_state_id bigint NOT NULL REFERENCES player_states(id),
  hero_def_id     int NOT NULL,
  level           int NOT NULL DEFAULT 1,
  stars           smallint NOT NULL DEFAULT 0,
  skills          jsonb NOT NULL,            -- {slot: level}
  shards          int NOT NULL DEFAULT 0,
  assigned_job    smallint,                  -- economy job slot
  UNIQUE (player_state_id, hero_def_id)
);

CREATE TABLE squads (
  id              bigserial PRIMARY KEY,
  player_state_id bigint NOT NULL REFERENCES player_states(id),
  slot_index      int NOT NULL,
  hero_ids        bigint[] NOT NULL,         -- exactly 3; [0] is leader
  troop_assign    jsonb NOT NULL,            -- {type_tier: count}
  UNIQUE (player_state_id, slot_index)
);

CREATE TABLE tech (
  player_state_id bigint NOT NULL REFERENCES player_states(id),
  node_id         int NOT NULL,
  level           int NOT NULL,
  PRIMARY KEY (player_state_id, node_id)
);
```

---

## 3. World

```sql
CREATE TABLE world_tiles (
  state_id        int NOT NULL,
  x               int NOT NULL,
  y               int NOT NULL,
  chunk_x         int NOT NULL,
  chunk_y         int NOT NULL,
  tile_type       smallint NOT NULL,
  occupant_type   smallint,
  occupant_id     bigint,
  level           int,
  payload         jsonb,
  version         int NOT NULL DEFAULT 1,
  PRIMARY KEY (state_id, x, y)
);
CREATE INDEX ix_tiles_chunk ON world_tiles(state_id, chunk_x, chunk_y);

CREATE TABLE marches (
  id              bigserial PRIMARY KEY,
  state_id        int NOT NULL,
  player_state_id bigint NOT NULL REFERENCES player_states(id),
  squad_id        bigint NOT NULL,
  march_type      smallint NOT NULL,
  from_x int, from_y int, to_x int, to_y int,
  departed_at     bigint NOT NULL,
  arrives_at      bigint NOT NULL,
  returns_at      bigint,
  status          smallint NOT NULL,          -- outbound|acting|returning|done
  rally_id        bigint,
  cargo           jsonb
);
CREATE INDEX ix_marches_arrives ON marches(arrives_at) WHERE status < 3;
CREATE INDEX ix_marches_player  ON marches(player_state_id);

CREATE TABLE battle_reports (
  id              bigserial PRIMARY KEY,
  state_id        int NOT NULL,
  attacker_id     bigint, defender_id bigint,
  kind            smallint NOT NULL,          -- pvp|zombie|rally|arena
  seed            bigint NOT NULL,
  outcome         smallint NOT NULL,
  snapshot        jsonb NOT NULL,             -- participants pre-battle
  round_log       jsonb NOT NULL,             -- replay source
  loot            jsonb,
  created_at      bigint NOT NULL
);
CREATE INDEX ix_reports_attacker ON battle_reports(attacker_id, created_at DESC);
CREATE INDEX ix_reports_defender ON battle_reports(defender_id, created_at DESC);
```

---

## 4. Alliances

```sql
CREATE TABLE alliances (
  id              bigserial PRIMARY KEY,
  state_id        int NOT NULL,
  tag             char(3) NOT NULL,
  name            text NOT NULL,
  level           int NOT NULL DEFAULT 1,
  tech_points     bigint NOT NULL DEFAULT 0,
  treasury        jsonb NOT NULL DEFAULT '{}',
  join_policy     smallint NOT NULL,
  banner          jsonb,
  war_wins        int NOT NULL DEFAULT 0,     -- ★ AvA record
  war_losses      int NOT NULL DEFAULT 0,
  war_streak      int NOT NULL DEFAULT 0,
  voice_enabled   boolean NOT NULL DEFAULT true,   -- ★ R5 kill switch
  created_at      bigint NOT NULL,
  UNIQUE (state_id, tag)
);

CREATE TABLE alliance_members (
  alliance_id     bigint NOT NULL REFERENCES alliances(id),
  player_state_id bigint NOT NULL REFERENCES player_states(id),
  rank            smallint NOT NULL,          -- 1..5
  joined_at       bigint NOT NULL,
  contribution    bigint NOT NULL DEFAULT 0,
  last_active_at  bigint,
  PRIMARY KEY (alliance_id, player_state_id)
);

CREATE TABLE alliance_help_requests (
  id              bigserial PRIMARY KEY,
  alliance_id     bigint NOT NULL,
  job_id          bigint NOT NULL REFERENCES jobs(id),
  requester_id    bigint NOT NULL,
  helps           int NOT NULL DEFAULT 0,
  expires_at      bigint NOT NULL
);
CREATE TABLE alliance_help_log (             -- prevents double-helping
  request_id      bigint NOT NULL,
  helper_id       bigint NOT NULL,
  helped_at       bigint NOT NULL,
  PRIMARY KEY (request_id, helper_id)
);

CREATE TABLE alliance_tech (
  alliance_id bigint NOT NULL, node_id int NOT NULL, level int NOT NULL,
  PRIMARY KEY (alliance_id, node_id)
);

CREATE TABLE alliance_gifts (
  id              bigserial PRIMARY KEY,
  alliance_id     bigint NOT NULL,
  tier            smallint NOT NULL,
  source_player   bigint,
  source_kind     smallint NOT NULL,          -- purchase|milestone|★ ava_win
  created_at      bigint NOT NULL,
  expires_at      bigint NOT NULL
);
CREATE TABLE alliance_gift_claims (
  gift_id bigint NOT NULL, player_state_id bigint NOT NULL, claimed_at bigint NOT NULL,
  PRIMARY KEY (gift_id, player_state_id)
);
```

---

## 5. ★ Arena

```sql
CREATE TABLE arena_rooms (
  id              bigserial PRIMARY KEY,
  state_id        int NOT NULL,
  alliance_id     bigint,                     -- sparring: owning alliance
  format_id       int NOT NULL,
  map_id          int NOT NULL,
  is_ava          boolean NOT NULL DEFAULT false,
  power_mode      smallint NOT NULL,          -- 0 normalized, 1 raw
  status          smallint NOT NULL,          -- lobby|running|finished|voided
  voice_room      text,                       -- ★ LiveKit room name
  created_by      bigint NOT NULL,
  created_at      bigint NOT NULL,
  starts_at       bigint,
  ended_at        bigint
);
CREATE INDEX ix_arena_alliance ON arena_rooms(alliance_id, status);

CREATE TABLE arena_matches (
  id              bigserial PRIMARY KEY,
  room_id         bigint NOT NULL REFERENCES arena_rooms(id),
  seed            bigint NOT NULL,
  tick_rate       smallint NOT NULL DEFAULT 20,
  initial_state   jsonb NOT NULL,
  input_log       bytea NOT NULL,             -- replay source, compact binary
  winner_side     smallint,
  duration_ticks  int,
  created_at      bigint NOT NULL
);

CREATE TABLE arena_participants (
  match_id        bigint NOT NULL REFERENCES arena_matches(id),
  player_state_id bigint NOT NULL,
  side            smallint NOT NULL,
  squad_snapshot  jsonb NOT NULL,
  elo_before      int, elo_after int,
  damage_dealt    bigint, kills int,
  disconnected    boolean NOT NULL DEFAULT false,
  PRIMARY KEY (match_id, player_state_id)
);

CREATE TABLE arena_elo (
  player_state_id bigint PRIMARY KEY REFERENCES player_states(id),
  elo             int NOT NULL DEFAULT 1200,
  peak_elo        int NOT NULL DEFAULT 1200,
  wins int NOT NULL DEFAULT 0, losses int NOT NULL DEFAULT 0,
  matches_today   int NOT NULL DEFAULT 0,
  week_id         int NOT NULL
);

CREATE TABLE ava_challenges (
  id              bigserial PRIMARY KEY,
  challenger_id   bigint NOT NULL, target_id bigint NOT NULL,
  format_id       int NOT NULL,
  scheduled_at    bigint NOT NULL,
  status          smallint NOT NULL,          -- pending|accepted|declined|done
  room_id         bigint,
  war_points_awarded int
);

CREATE TABLE alliance_champions (            -- ★ Hall of Fame
  alliance_id bigint NOT NULL, week_id int NOT NULL,
  player_state_id bigint NOT NULL, elo int NOT NULL,
  PRIMARY KEY (alliance_id, week_id)
);
```

---

## 6. ★ Voice

```sql
CREATE TABLE voice_sessions (
  id              bigserial PRIMARY KEY,
  player_state_id bigint NOT NULL,
  room_name       text NOT NULL,
  room_kind       smallint NOT NULL,          -- alliance|officer|rally|arena|war|squad
  joined_at       bigint NOT NULL,
  left_at         bigint
);
CREATE INDEX ix_voice_player ON voice_sessions(player_state_id, joined_at DESC);

CREATE TABLE voice_moderation (
  id              bigserial PRIMARY KEY,
  target_player   uuid NOT NULL,
  actor_player    uuid,                       -- NULL = system
  action          smallint NOT NULL,          -- warn|mute|ban levels
  reason          text,
  evidence_ref    text,                       -- reported clip key
  expires_at      bigint,
  created_at      bigint NOT NULL
);

CREATE TABLE voice_reports (
  id              bigserial PRIMARY KEY,
  reporter        uuid NOT NULL,
  reported        uuid NOT NULL,
  room_name       text NOT NULL,
  clip_key        text,                       -- object storage key
  clip_expires_at bigint NOT NULL,            -- auto-delete at +7 days
  status          smallint NOT NULL,
  created_at      bigint NOT NULL
);
CREATE INDEX ix_vreports_status ON voice_reports(status, created_at);
```

**Retention job:** a nightly task deletes any `voice_reports` clip past `clip_expires_at` and nulls `clip_key`. This is a legal requirement (`10 §6.4`), not an optimisation.

---

## 7. Events, chat, economy

```sql
CREATE TABLE event_definitions (
  id text PRIMARY KEY, definition jsonb NOT NULL,
  starts_at bigint, ends_at bigint, enabled boolean NOT NULL DEFAULT false
);
CREATE TABLE event_scores (
  event_id text NOT NULL, phase_id text NOT NULL,
  scope smallint NOT NULL,                    -- player|alliance
  scope_id bigint NOT NULL,
  points bigint NOT NULL DEFAULT 0,
  claimed_milestones int[] NOT NULL DEFAULT '{}',
  PRIMARY KEY (event_id, phase_id, scope, scope_id)
);
CREATE INDEX ix_escores_rank ON event_scores(event_id, phase_id, points DESC);

CREATE TABLE chat_messages (
  id bigserial PRIMARY KEY,
  channel smallint NOT NULL, scope_id bigint,
  player_state_id bigint, body text NOT NULL,
  lang char(2), created_at bigint NOT NULL,
  deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX ix_chat_scope ON chat_messages(channel, scope_id, created_at DESC);

CREATE TABLE mail (
  id bigserial PRIMARY KEY, player_state_id bigint NOT NULL,
  category smallint NOT NULL, subject text, body text,
  attachments jsonb, read_at bigint, claimed_at bigint,
  created_at bigint NOT NULL, expires_at bigint
);

CREATE TABLE purchases (
  id bigserial PRIMARY KEY,
  player_id uuid NOT NULL, product_id text NOT NULL,
  store text NOT NULL, order_id text UNIQUE NOT NULL,
  price_micros bigint, currency char(3),
  validated boolean NOT NULL DEFAULT false,
  granted_at bigint, refunded_at bigint,
  receipt jsonb NOT NULL, created_at bigint NOT NULL
);

CREATE TABLE currency_audit (               -- append-only
  id bigserial PRIMARY KEY,
  player_state_id bigint NOT NULL,
  currency text NOT NULL, delta bigint NOT NULL,
  balance_after bigint NOT NULL,
  reason text NOT NULL, ref_id text,
  created_at bigint NOT NULL
);
CREATE INDEX ix_audit_player ON currency_audit(player_state_id, created_at DESC);

CREATE TABLE admin_audit (                  -- append-only, every admin action
  id bigserial PRIMARY KEY,
  admin_user text NOT NULL, action text NOT NULL,
  target text, payload jsonb, ip inet,
  created_at bigint NOT NULL
);
```

**`currency_audit` is not optional.** Without a full ledger you cannot investigate an exploit, answer a refund dispute, or prove a balance is correct. Every grant, spend, and refund writes a row.

---

## 8. Redis key layout

| Key | Type | Purpose | TTL |
|---|---|---|---|
| `ps:{id}` | Hash | Cached player state | 300 s |
| `chunk:{state}:{cx}:{cy}` | Hash | World chunk snapshot | 60 s |
| `sched:jobs` | ZSet | Job completions by timestamp | — |
| `sched:marches` | ZSet | March arrivals by timestamp | — |
| `lb:{state}:power` | ZSet | Power leaderboard | — |
| `lb:elo:{alliance}` | ZSet | ★ Sparring ladder | — |
| `arena:{matchId}` | Hash | ★ Live match state | match + 60 s |
| `tr:{hash}:{lang}` | String | Translation cache | 7 d |
| `rl:{playerId}:{bucket}` | String | Rate-limit counter | 60 s |
| `presence:{state}` | Set | Online players | 30 s heartbeat |

**Redis holds nothing that cannot be rebuilt from Postgres.** If Redis is wiped, the game continues at reduced speed. That property is what makes it safe to run both on one box.

---

## 9. Migration & backup

- **EF Core migrations**, checked into git, applied on deploy
- Every migration must be **backwards-compatible for one version** so a rollback doesn't corrupt data
- **Backups:** nightly `pg_dump` → compressed → off-VM (Backblaze B2 free tier, 10 GB)
- Retention: 7 daily, 4 weekly, 3 monthly
- **A backup you have not restored is not a backup** — restore drill monthly, documented in `24`

---

## Next
- `16-netcode-realtime.md`
- `20-security-anticheat.md`
- `24-devops-deployment.md`

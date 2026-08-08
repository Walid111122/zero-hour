# 07 — GDD: World Map & Marches

> The world layer turns a single-player builder into a shared place. It is where conflict, territory, and social pressure live.
> Phase 4.

---

## 1. The State (server world)

| Property | v1 value | Rationale |
|---|---|---|
| Grid size | **512 × 512** tiles | ~262k tiles; dense enough for real conflict at low DAU |
| Target population | ~5,000 players | A 1024² map with 500 players feels dead |
| States | Opens on schedule, closes registration when full | Creates land-rush urgency |
| Coordinates | `(x, y)` integers, shown as `X:123 Y:456` | Players share these constantly in chat |
| Zones | 5 concentric rings, difficulty rises outward | Natural progression gradient |

### Zones
| Ring | Tiles from centre | Node levels | Zombie levels | Notes |
|---|---|---|---|---|
| Outer (spawn) | 200+ | 1–3 | 1–6 | New players spawn here, safe-ish |
| Mid | 140–200 | 3–6 | 6–14 | |
| Inner | 80–140 | 6–8 | 14–22 | Contested |
| Core | 30–80 | 8–10 | 22–30 | Gold mines, alliance territory |
| **Capitol** | centre 30 | — | — | Capitol Clash target (Phase 6) |

---

## 2. Tile contents

| Tile type | Occupies | Yields |
|---|---|---|
| Empty terrain | — | Movement only |
| **Player base** | 1 tile | Attack/scout target |
| **Resource node** (Food / Iron) L1–L10 | 1 tile | Gathered over time by an occupying march |
| **Zombie unit** L1–L30 | 1 tile | One-shot battle → Hero EXP, chests, event points |
| **Elite zombie** | 1 tile | Rally target, rare materials |
| **Gold Mine / Oil Rig** | 2×2 | Continuous alliance yield while held |
| **Alliance flag / turret** | 1 tile | Territory claim |
| Impassable (ruins, water) | 1 tile | Blocks pathing, creates chokepoints |

Chokepoints are placed deliberately. A map with no terrain is a map with no strategy.

---

## 3. Marches

### 3.1 Rules
```
MarchQueues = 2 base, +1 at HQ 10, +1 via tech, +1 via VIP 10   (max 5)
TravelTime  = ceil(chebyshevDistance / marchSpeed) seconds
marchSpeed  = base · (1 + techSpeed% + heroSpeed% + vipSpeed% + terrainMod)
```

March types: **Gather** · **Attack** · **Scout** · **Rally (join/lead)** · **Reinforce** · **Transport**

- A march carries one squad (3 heroes + assigned troops)
- **Recall** available any time; return travel is the same duration
- Marches survive logout entirely (server-side, lazily resolved)

### 3.2 Lazy resolution again
A march is a database row: `{ squadId, fromTile, toTile, type, departedAt, arrivesAt }`.

Nothing ticks. On read — or when the arrival job fires from the Redis sorted set — the server resolves it. 50,000 in-flight marches cost nothing until they land. Same principle as building timers (`04 §6`).

---

## 4. Gathering

```
GatherRate  = nodeBaseRate(level) · (1 + gatherTech% + heroGatherSkill%)
Capacity    = squadMarchCapacity
Duration    = min(Capacity / GatherRate, nodeRemaining / GatherRate)
```
- Node depletes and despawns; a new node spawns elsewhere in the ring (population stays constant)
- A gathering march **can be attacked** — its carried resources are the prize. This is the primary source of organic PvP, and it's better than base-attacking because the victim loses cargo, not their home.

---

## 5. Zombie hunting

- Costs **Stamina** (100 cap, +1 per 5 min, refillable with diamonds)
- One-shot resolved battle, minimal troop loss by design
- Yields: Hero EXP (primary), chests, event points, occasional shards
- Levels 1–30 gated by your highest cleared level +2 (prevents a new player suiciding into L30)

**Stamina is the session-length limiter.** It is also the most common first diamond purchase, because refilling it is cheap and the payoff is immediate.

---

## 6. PvP

### 6.1 Flow
```
Find target (map / rank list / alliance intel)
   ↓ Scout  (costs 1 Scout Report, ~30 s flight)
   ↓ Read intel: troop counts, hero levels, wall level, unprotected resources
   ↓ Attack  (send squad, flight time by distance)
   ↓ Resolved battle (server, deterministic — see 05 §5.2)
   ↓ BattleReport to both players + loot returned home
```

### 6.2 The three protections (Pillar 3)
| Protection | Effect |
|---|---|
| **Warehouse** | `warehouseProtected` resources are untouchable |
| **Wounded rule** | 70–90% of losses are healable, not dead |
| **Shields** | Absolute immunity for 8 h / 24 h / 3 d |

**Shield policy:**
- New players: **72 h free shield** on account creation
- After any defeat: **2 h free grace shield** (auto-applied, cannot be declined)
- Purchasable/earnable thereafter
- **Shield breaks if you send an attack march** — you cannot hide and hit

The auto-grace-shield after a defeat is important. It stops "farm-lock" bullying where a strong player repeatedly hits the same weak base, which is the single most common cause of early churn in this genre.

### 6.3 Attack restrictions
- Cannot attack a base more than **2× below your power** without a penalty (no loot, no rank, alliance-visible shame log)
- Cannot attack the same target more than **3× per 24 h**
- Cannot attack alliance members (that's what ★ arena sparring is for — see `11`)

---

## 7. Rallies

```
Leader starts a rally → sets a 5 / 15 / 30 min gather window
   → members join with their squads (troops combine)
   → at window close, the combined force marches as one
   → resolved as a single battle; losses distributed proportionally
```
Rally capacity is gated by the leader's Alliance Center level. Rallies are required for Elite zombies and effective against strong bases.

**★ Voice integration:** starting a rally auto-creates an ad-hoc voice channel and invites joiners. See `10 §3`.

---

## 8. Alliance territory

- Alliance flags claim tiles; adjacent tiles can be claimed to expand contiguously
- Turrets defend territory and add buffs
- Inside own territory: **+10% march speed, +5% defense**
- Gold Mines/Oil Rigs inside territory yield to the alliance treasury
- Territory can be contested by destroying flags/turrets

---

## 9. Teleports

| Item | Effect |
|---|---|
| Random Teleport | Move to a random tile in your current ring |
| Advanced Teleport | Move to chosen coordinates (if empty) |
| Alliance Teleport | Move adjacent to an alliance member |

Teleports are a major convenience item and a genuine strategic tool (mass-teleporting an alliance into position before a war). Available free at low rates and purchasable.

---

## 10. Technical design

### 10.1 Client streaming
- World is chunked into **32×32 tile chunks** (256 chunks total)
- Client requests visible chunks + 1 ring of margin
- Chunk payload: tile types, occupant summaries (id, name, alliance tag, power band)
- Updates via **WebSocket delta push** for chunks the client currently subscribes to
- Chunks cached client-side with a version stamp

### 10.2 Server spatial index
- Redis: `GEO`-style or a simple `chunk:{cx}:{cy}` hash → occupant list
- Postgres: `world_tiles` table with a composite index on `(chunk_x, chunk_y)`
- Occupancy changes publish to a Redis channel per chunk; the gateway fans out to subscribed clients

### 10.3 Rendering budget
| Metric | Budget |
|---|---|
| Visible tiles | ~600 at default zoom |
| Draw calls | < 80 |
| Base/unit meshes | **GPU instanced**, one mesh per category |
| Name labels | Pooled, culled aggressively, LOD'd out when zoomed far |
| Allocations per frame | 0 |

At far zoom the map renders as a **single procedural texture** built from chunk data rather than thousands of objects. This is the difference between a smooth map and a slideshow.

---

## 11. Data files

| File | Contents |
|---|---|
| `world_config.csv` | grid size, ring radii, node density, respawn rules |
| `nodes.csv` | nodeLevel, resourceType, totalAmount, baseGatherRate |
| `zombies.csv` | level, power, stamina cost, rewards, isElite |
| `march_config.csv` | base speed, queue unlocks, capacity formula |
| `shields.csv` | shieldId, durationHours, source |
| `teleports.csv` | itemId, type, restrictions |

---

## 12. Phase 4 acceptance criteria
- [ ] 512² map generates with correct ring distribution
- [ ] Chunk streaming smooth at all zoom levels, 60 fps
- [ ] Seamless base ⇄ world zoom works both directions
- [ ] Marches: all 6 types, queues enforced server-side
- [ ] Gathering with interception PvP working
- [ ] Zombie hunting with stamina
- [ ] Scout → attack → report → loot full loop
- [ ] All three protections verified (warehouse, wounded, shields)
- [ ] Auto grace-shield fires after every defeat
- [ ] Rallies with combined forces
- [ ] Territory claim/contest
- [ ] 100 simulated players on one map with no desync

---

## Next
- `08-GDD-alliance-social.md`
- `10-FEATURE-voice-chat.md` ★
- `11-FEATURE-arena-rooms.md` ★

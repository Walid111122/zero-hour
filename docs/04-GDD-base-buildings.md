# 04 — GDD: Base & Buildings

> The base layer creates **frequency**. Timers make appointments; appointments make habit.
> All numbers here are **starting values** in `tools/balance/*.csv`, tuned in Phase 9.

---

## 1. Base layout

- **Grid:** 12 × 12 logical tiles, isometric 2.5D presentation
- **Camera:** fixed 45° isometric, orthographic, pinch-zoom + drag-pan
- **Buildings occupy** 1×1, 2×2, or 3×3 tiles
- **Slots are pre-placed** in v1 (no free placement). Reason: free placement doubles the UI work, creates broken layouts, and adds nothing players value in this genre. Decorative placement can come later.
- **Outside the wall:** a small decorative rubble zone that expands visually as HQ levels rise. Cheap, high-impact sense of growth.

### Zoom levels
| Zoom | View |
|---|---|
| 0.6× | Full base + surroundings |
| 1.0× | Default — base fills screen |
| 1.6× | Close on one building |
| **Pinch past 0.4×** | **Seamless transition to world map** (see §7) |

---

## 2. Building roster (12 at launch)

| # | Building | Size | Unlock | Function |
|---|---|---|---|---|
| 1 | **Headquarters** | 3×3 | start | Caps all other buildings; unlocks features |
| 2 | **Farm** | 2×2 | start | Produces Food |
| 3 | **Iron Mine** | 2×2 | HQ 2 | Produces Iron |
| 4 | **Bank** | 2×2 | HQ 4 | Produces Coin |
| 5 | **Warehouse** | 2×2 | HQ 3 | Protected resource capacity |
| 6 | **Armored Factory** | 2×2 | HQ 3 | Trains Tank troops |
| 7 | **Air Base** | 2×2 | HQ 6 | Trains Air troops |
| 8 | **Missile Silo** | 2×2 | HQ 9 | Trains Missile troops |
| 9 | **Hospital** | 2×2 | HQ 5 | Heals wounded troops |
| 10 | **Tech Center** | 2×2 | HQ 4 | Research tree |
| 11 | **Hero Center** | 2×2 | HQ 3 | Gacha + hero management |
| 12 | **Alliance Center** | 2×2 | HQ 5 | Help, gifts, donations, ★ voice, ★ arena entry |
| — | **Wall** | perimeter | start | Base defense; visual level indicator |

**Deferred to Phase 6+:** Drone Command, Truck Depot, Trade Center, Radar Station, Drill Ground.

### Why the Alliance Center is the arena/voice entry point
Both new features are alliance-scoped, so putting their entry in the Alliance Center building is discoverable and thematically correct. It also means a player must have joined an alliance before they see them — which is exactly the funnel we want.

---

## 3. HQ gating — the spine

```
Rule 1: BuildingLevel ≤ HQLevel                    (hard cap, server-enforced)
Rule 2: HQ upgrade requires N buildings at HQLevel  (forces broad investment)
Rule 3: HQ level unlocks features                   (the reveal schedule)
```

### Feature unlock schedule
| HQ | Unlocks |
|---|---|
| 1 | Base, Farm, runner stages |
| 2 | Iron Mine, daily missions |
| 3 | Warehouse, Armored Factory, Hero Center, first gacha pull |
| 4 | Bank, Tech Center |
| **5** | **World map, marches, Alliance Center, Hospital** |
| 6 | Air Base, PvP attack, ★ **voice chat** |
| 7 | Rallies, alliance tech |
| **8** | ★ **Arena sparring (intra-alliance)** |
| 9 | Missile Silo, second build queue (tech-gated) |
| 10 | Arms Race, alliance territory |
| 12 | Alliance Duel, ★ **AvA arena rooms** |
| 15 | Crazy Joe, hero star-up |
| 18 | Hero skills tier 3, tech branch 3 |
| 20 | Capitol Clash |
| 25 | Season leaderboards |
| 30 | Endgame cap for v1 |

### HQ upgrade requirements (sample)
| HQ → | Requires |
|---|---|
| 2 | Farm 1 |
| 5 | Farm 4, Iron Mine 4, Warehouse 4 |
| 10 | Farm 9, Iron 9, Bank 9, Tech 9, Armored 9 |
| 20 | All unlocked buildings at 19 |
| 30 | All unlocked buildings at 29 |

Full table: `tools/balance/hq_requirements.csv`

---

## 4. Resources

### Launch set (6 currencies — deliberately fewer than the reference game)
| Resource | Type | Produced by | Spent on | Lootable |
|---|---|---|---|---|
| **Food** | Soft bulk | Farm, nodes, runner | Building, training, healing | Yes (above warehouse) |
| **Iron** | Soft bulk | Iron Mine, nodes, runner | Building, research | Yes (above warehouse) |
| **Coin** | Soft mid | Bank, events | Research, heroes | Yes (above warehouse) |
| **Diamonds** | 💎 Premium | IAP, events, missions | Speedups, gacha, packs | **No** |
| **Speedups** | Item | Events, missions, IAP | Time reduction | **No** |
| **Valor Badges** | Alliance | Help, alliance events, ★ arena | Alliance shop | **No** |

Plus non-currency progression items: Hero EXP, Hero Shards, Skill Books, Tech Points.

### Production formula
```
ProductionPerHour(building, L) = base · L^1.35 · (1 + techBonus + vipBonus + allianceTechBonus)
StorageCapacity(building, L)   = prodPerHour · 8          # 8 hours to fill
WarehouseProtected(L)          = wpBase · L^1.5           # cannot be looted
```

### Collection
- Building fills over time; a glowing icon appears when > 10% full
- Tap to collect; auto-collect on `CollectAll` button (unlocked HQ 4, **free**)
- **Overflow is wasted** — this is what creates the "come back and collect" pressure. Do not silently bank overflow.

### The looting rule (Pillar 3 in action)
```
Lootable = max(0, currentResource - warehouseProtected)
AttackerTakes = min(Lootable · 0.5, attackerMarchCapacity)
```
A well-built warehouse means a raid costs you a slice, never your foundation.

---

## 5. Build timers — the product

```
BuildTime(L)  = 20s · 1.42^L        capped at 4 days
BuildCost(L)  = baseCost · 1.55^L
```

| HQ level | Time (raw) | With 30 helps + tech + VIP ⚠️ |
|---|---|---|
| 1–5 | 20 s – 2 min | instant-ish |
| 6–10 | 5 min – 45 min | 3 – 30 min |
| 11–15 | 1.5 h – 8 h | 1 – 5 h |
| 16–20 | 12 h – 1.5 d | 7 h – 22 h |
| 21–25 | 2 d – 3 d | 1.2 d – 1.8 d |
| 26–30 | 3.5 d – **4 d (cap)** | 2 d – 2.4 d |

**The 4-day cap is a deliberate divergence from the reference game.** A 20-day timer only works with a huge population and enormous UA spend to replace the players it drives away. At our scale, 4 days is the ceiling before frustration exceeds anticipation.

### Time reduction stack
| Lever | Effect | Source |
|---|---|---|
| **Alliance Help** | −1% remaining per click, **max 30 clicks (−30%)** | Free, social |
| Speedup items | Fixed subtraction (1m/5m/1h/8h/24h) | Events, missions, IAP |
| Construction tech | up to −20% | Tech tree |
| VIP | up to −15% | VIP ladder |
| Diamonds | Instant finish, cost scales with remaining | Premium |

**Alliance Help is the keystone.** It is free, it requires other people, and it is worth ~30% of your progression speed. This is what makes joining an alliance mechanically mandatory — which is exactly what we want, because our differentiators live in the alliance layer.

### Build queues
| Queue | Unlock |
|---|---|
| Queue 1 | Start |
| Queue 2 | HQ 9 + Tech "Dual Construction", **or** VIP 6 |
| Queue 3 | Tech tier 3 + VIP 12 |

Both paths to queue 2 exist on purpose: a free path (tech) and a paid path (VIP). Pillar 4.

---

## 6. Server-authoritative timers with lazy evaluation

**This is the single most important implementation detail in the base layer.** It is what allows a free 4-core VM to host thousands of players.

### Never do this
```
every second: for each base in world: base.resources += rate   // 40,000 writes/sec 💀
```

### Do this
```csharp
// Resolve on read only
long elapsed = now - state.LastResolvedAt;
state.Food = Min(cap, state.Food + rate.Food * elapsed);
state.LastResolvedAt = now;
// Timers: a build is complete iff now >= build.CompletesAt. No tick needed.
```

**Consequences:**
- Server cost is proportional to **active players**, not total players
- Timers are exact and clock-change-proof (server time only)
- Offline income is the same code path as online income — one formula, no divergence bugs

Client mirrors the same `shared/Sim` formula for prediction, then reconciles against server truth. Because the formula is literally the same compiled code, they cannot drift.

### Anti-cheat
- Server never trusts a client-reported timestamp
- "Build complete" is a server assertion, not a client claim
- Device clock changes have zero effect
- Full threat model in `20-security-anticheat.md`

---

## 7. The seamless base ⇄ world transition

The signature interaction. Worth building properly.

```
Base at zoom 1.0
  ↓ pinch out
zoom 0.6 — full base visible
  ↓ continue
zoom 0.4 — THRESHOLD: base LOD swaps to a single "base tile" mesh,
           world map chunks begin streaming in around it,
           camera continues rising with no cut
  ↓ continue
zoom 0.2 — world map view, your base is one tile among many
```

**Implementation notes:**
- One camera, one continuous animation curve. Not two scenes with a fade.
- Base detail prefabs unload and the simplified base-tile prefab loads at the threshold
- World chunks preload starting at zoom 0.5 so nothing pops in
- Reverse direction is symmetric
- Budget: **1 full week** of Phase 2. It looks simple and is not.

**Why it matters:** it makes the base game and the world game feel like one place instead of two menus. Every competitor that uses a loading screen here feels worse, and players cannot articulate why.

---

## 8. Wall & defense

- Wall level = HQ level (auto, not separately upgraded — one less chore)
- Provides base defense stat and a **visual growth indicator** (the most-seen art asset in the game)
- Turret slots at HQ 10 / 15 / 20; turrets add flat defense
- Base defense contributes to the defender's side in PvP resolution

---

## 9. Hospital & the wounded rule

```
On defeat:
  wounded = min(losses · woundRate, hospitalCapacity)
  dead    = losses - wounded
  woundRate = 0.7 base, up to 0.9 with tech/hero bonuses
  HealCost = trainCost · 0.25
  HealTime = trainTime · 0.15
```

- Hospital capacity scales with level; overflow becomes dead
- **A player who loses a battle keeps ~70–90% of their army.** This is Pillar 3 and it is not negotiable.
- Healing is a resource sink that feels like recovery rather than punishment — good design and good economy at once.

---

## 10. Red-dot notification tree

Formal from day one. Retrofitting ad-hoc booleans is a nightmare.

```
Root
├─ Base
│  ├─ Collectible resource (any building > 10% full)
│  ├─ Completed build / research / training
│  ├─ Empty build queue          ← the churn-prevention signal
│  └─ Upgradeable building (affordable now)
├─ Heroes (levelable / star-up / free pull available)
├─ Alliance
│  ├─ Help requests pending
│  ├─ Unclaimed gifts
│  ├─ ★ Voice channel active (live indicator, not a dot)
│  └─ ★ Arena invite / AvA challenge pending
├─ Events (unclaimed rewards, phase change imminent)
├─ Mail (unread)
└─ Shop (free daily item available)
```

Rules:
- A node is "dotted" if **any** descendant is dotted
- Recomputed on state change, never polled per frame
- Counts shown where meaningful; plain dot otherwise
- **★ Voice uses a live animated indicator, not a red dot** — a dot implies an unclaimed reward, and voice activity is different information

Implementation detail in `19-ux-ui-system.md`.

---

## 11. Balance data files

| File | Contents |
|---|---|
| `buildings.csv` | id, name, size, unlockHQ, maxLevel, category |
| `building_levels.csv` | buildingId, level, foodCost, ironCost, coinCost, buildSeconds, prodPerHour, capacity, power |
| `hq_requirements.csv` | hqLevel, requiredBuildingId, requiredLevel |
| `unlocks.csv` | hqLevel, featureId |
| `resources.csv` | id, name, isPremium, isLootable, capFormula |

Codegen produces strongly-typed C# tables into `shared/Sim/Generated/`. Server can hot-reload; client receives a config version and re-fetches on mismatch.

---

## Next
- `05-GDD-troops-heroes-combat.md`
- `12-economy-balance-model.md` — the formulas above, tuned
- `19-ux-ui-system.md` — red-dot implementation, juice

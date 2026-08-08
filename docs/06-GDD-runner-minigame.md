# 06 — GDD: The Runner Minigame ("Doomsday Squad")

> **This is Phase 1. It is the first thing we build and the first thing we ship.**
> It is a complete standalone game, the acquisition hook, and the host of the idle economy.

---

## 1. What it is

A squad of soldiers auto-runs forward down a 3-lane corridor. The player drags left and right. The corridor contains gates that change the squad, enemies that shoot back, and a boss at the end.

**Duration:** 45–75 seconds per stage.
**Input:** one finger, horizontal drag. Nothing else.
**Skill expression:** choosing the right gate.

---

## 2. Why it's built first

| Reason | |
|---|---|
| **It's the ad** | The trailer *is* the gameplay, so install→play conversion stays high and reviews don't punish us |
| **It's shippable alone** | A real product in 4–6 weeks. Validates the hook for $0 |
| **It's the tutorial** | Teaches nothing about 4X, earns investment before complexity arrives |
| **It hosts the idle economy** | Highest stage cleared drives offline income, which is the D1 retention engine |
| **It's cheap content** | New stages are CSV rows, not art |
| **It de-risks everything** | If the runner isn't fun, we learn that in week 6 instead of month 14 |

---

## 3. Playfield

```
        ┌───┬───┬───┐   ← corridor, 3 lanes, 6 world units wide
        │   │   │   │
        │ [+12] [×2]│   ← gate pair (a choice, always 2 options)
        │   │   │   │
        │  ▼ ▼ ▼    │   ← zombies advancing
        │           │
        │  ███████  │   ← squad (player), auto-firing forward
        └───┴───┴───┘
             ▲
          camera: behind + above, slight tilt
```

- Squad auto-advances at a constant `runSpeed` (world units/sec)
- Player drags to set target X within lane bounds; squad lerps toward it
- Squad is a **formation of N soldiers** in a grid; N is the core number the whole game is about
- Camera follows on Z, fixed X/Y, subtle FOV punch on big events

---

## 4. Gates — the heart of the game

### 4.1 Rules
- Gates **always appear in pairs** (occasionally triples), spanning the corridor
- Player passes through exactly one
- Passing applies the operator instantly with a loud, unmissable visual + audio confirmation
- Gate spacing: **every 2.5–4 seconds** of travel

### 4.2 Operators

| Operator | Effect | Colour | Notes |
|---|---|---|---|
| `+N` | Add N soldiers | Green | Bread-and-butter |
| `×N` | Multiply squad by N | **Gold** | The dopamine hit. N ∈ {1.5, 2, 3} |
| `−N` | Remove N soldiers | Red | The bad option in a pair |
| `÷N` | Divide squad by N | Dark red | Punishing |
| `⚔ Weapon+` | Upgrade weapon tier | Blue | Damage, not count |
| `⇄ Type` | Switch troop type | Purple | Matters for the boss counter |
| `♥ Shield` | Temporary damage immunity | Cyan | Rescue mechanic |

### 4.3 The choice must be real
A gate pair is only interesting if the answer is not obvious. Design rules:

- **Never** pair `+12` with `−5`. That's not a choice, it's a formality.
- **Do** pair `×2` with `+40` — the answer depends on your current squad size. With 10 soldiers, `+40` wins. With 40, `×2` wins. **The player must do arithmetic under time pressure.** That is the game.
- **Do** pair `×3` with `⚔ Weapon+` — count versus damage, a genuine strategic trade
- **Do** pair `+30` with `⇄ Type` when the upcoming boss has a type weakness — rewards knowledge
- Roughly **20%** of pairs may be "obvious" as a breather. Not more.

```
BreakEvenSize(×m vs +a) = a / (m - 1)
   e.g. ×2 vs +40  →  break-even at 40 soldiers
```
The stage generator uses this to place pairs where the break-even point sits near the player's *expected* squad size at that moment — which is exactly where the decision is hardest and most interesting.

---

## 5. Enemies

| Type | Behaviour |
|---|---|
| **Runner zombie** | Charges the squad, low HP, appears in clumps |
| **Shooter zombie** | Stops and fires, must be prioritised |
| **Armored zombie** | High HP, slow, requires damage not count |
| **Mid-boss** | Blocks the lane at ~60% progress, moderate HP bar |
| **Wall / Final boss** | End of stage, large HP bar, has a **troop type** → the counter triangle applies |

### Combat resolution (runner)
```
SquadDPS   = soldierCount · weaponDamage(tier) · fireRate · counterMult(squadType, enemyType)
EnemyDPS   = Σ activeEnemies.damage
soldierLoss = enemyDamage / soldierHP        (fractional, accumulated)
```
Soldiers die individually with visible pops. Losing soldiers must be *legible* — the player should always see the count drop and know why.

---

## 6. Stage structure

```
0%   ── spawn, brief clear runway (1.5 s to orient)
10%  ── gate pair 1
20%  ── first zombie clump
30%  ── gate pair 2
40%  ── shooter zombies
50%  ── gate pair 3  (often ×N — the mid-stage power spike)
60%  ── MID-BOSS
70%  ── gate pair 4
80%  ── mixed wave
90%  ── gate pair 5  (last chance to prepare)
100% ── FINAL BOSS / WALL
```

Timing target: **55 s median**, 45 s floor, 75 s ceiling.

### 6.1 Difficulty curve

```
BossHP(stage)      = 100 · 1.155^stage
WaveDensity(stage) = 1 + stage · 0.04
GateGenerosity(s)  = decreases slowly with stage
```

**Expected player power** (assuming they buy affordable upgrades):
```
ExpectedPower(s) = startSoldiers · avgGateMultiplier^gateCount · weaponMult(upgrades)
```

Tuning goal: `ExpectedPower(s) / RequiredPower(s)` oscillates around **1.1**, dipping **below 1.0 every 4–6 stages**. Those dips are the intentional walls.

### 6.2 The wall is the funnel
When a player stalls, the resolutions are:
1. **Play better** — free, immediate, skill-based (the honest option)
2. **Spend idle income on upgrades** — free, slow (the default path)
3. **Rewarded ad for a temporary boost** — free, instant (once monetization exists)
4. **Spend diamonds** — paid, instant

**Design ethic:** option 1 must always be genuinely viable. A wall that *cannot* be beaten by good play is a paywall, and paywalls in the first hour destroy retention. See Pillar 4 in `02`.

### 6.3 Stage count
- **Launch: 60 stages** (about 2 hours of first-clear content)
- Every 10 stages: a **chapter** with a themed environment tint and a bigger boss
- Post-60: **endless mode** with procedural scaling for the idle rate

---

## 7. Permanent upgrades

Bought with **Food** (the runner's own soft currency), which comes from stage clears and idle income.

| Upgrade | Effect | Levels | Cost curve |
|---|---|---|---|
| **Squad Size** | +1 starting soldier | 50 | `100 · 1.18^L` |
| **Weapon Damage** | +8% damage | 100 | `120 · 1.16^L` |
| **Fire Rate** | +5% fire rate | 40 | `200 · 1.22^L` |
| **Soldier HP** | +10% soldier HP | 50 | `150 · 1.19^L` |
| **Idle Rate** | +10% offline income | 60 | `300 · 1.21^L` |
| **Idle Cap** | +15 min offline cap | 20 | `500 · 1.3^L` |

**One-tap "buy max"** on every upgrade. Making players tap 40 times is not engagement, it is friction.

---

## 8. Idle income — the D1 retention engine

```
IdleRatePerHour = baseRate · 1.14^highestStageCleared · (1 + idleUpgradeBonus)
IdleCap         = 3 h  (+15 min per Idle Cap level, max 8 h)
Accrued         = min(IdleRatePerHour · hoursOffline, IdleRatePerHour · IdleCap)
```

### The return moment
On app open, if `Accrued > 0`:
1. Full-screen popup, dark backdrop
2. "While you were away — 2h 47m"
3. Resource icons with counting-up numbers
4. **[Collect]** and **[Collect ×2 — watch ad]**
5. Coins fly to the HUD, HUD counts up, chime

This popup is the **most important 4 seconds in the game.** It is the payoff for returning, and it must feel generous every single time.

### Why the cap exists
An uncapped idle game has no reason to be opened more than once a day. A 3-hour cap creates **three to five natural check-ins per day**. The cap is the retention mechanic; the income is just the reward.

---

## 9. Meta integration (Phase 2+)

Once the base layer exists:
- Runner stage clears yield **Food + Iron + Hero EXP** into the main account
- The runner squad's visuals reflect your **actual highest troop tier**
- **Squad Size** upgrade cap raises with HQ level (ties the two games together)
- Idle income feeds the base economy, not a separate wallet
- The runner remains permanently available, not a one-time tutorial

**Critical:** the runner must never become vestigial. It stays a first-class mode with its own daily rewards and Arms Race phase.

---

## 10. Feel & juice checklist

This list is the difference between "a runner" and "a good runner". Each item is cheap and each one matters.

**Gate pass**
- [ ] Gate glass shatters into particles
- [ ] Number punches in scale, then settles
- [ ] Distinct SFX per operator type (`×N` gets the best one)
- [ ] Squad count on HUD counts up rapidly, doesn't snap
- [ ] Brief slow-mo (0.85× for 0.15 s) on `×N` gates only

**Soldiers**
- [ ] New soldiers spawn with a pop + small dust puff
- [ ] Formation reflows smoothly, never teleports
- [ ] Muzzle flashes, tracers, shell casings
- [ ] Slight per-soldier run-cycle offset so the formation isn't robotic

**Combat**
- [ ] Enemy hit flash (white, 60 ms)
- [ ] Damage numbers, small and fast
- [ ] Enemy death: ragdoll or pop + particles
- [ ] Screen shake on boss hits (subtle — 2 px, 80 ms)
- [ ] Haptic tap on gate pass and boss kill (Android vibration)

**Boss**
- [ ] Name banner slides in
- [ ] HP bar with a chunky segmented fill
- [ ] Camera pushes in slightly
- [ ] Music intensity layer added
- [ ] Death: slow-mo + burst + coin fountain

**Results**
- [ ] Stars (1–3) based on remaining squad %
- [ ] Rewards fly out sequentially, not all at once
- [ ] Next-stage button pre-highlighted (keep momentum)

**Failure**
- [ ] Never say "You Lose". Say **"Almost!"** with the boss HP % shown
- [ ] Immediate **[Retry]**, no interstitial
- [ ] Show what an upgrade would have done — a concrete nudge, not a nag

---

## 11. Technical design (Unity)

### 11.1 Scene composition
Built by an Editor generator (`Zero Hour ▸ Generate ▸ Runner Scene`), not hand-authored YAML.

```
RunnerScene
├─ Bootstrap            (RunnerGameController)
├─ Camera               (follow rig)
├─ Environment          (pooled corridor segments, chapter tinting)
├─ Squad                (SquadController, formation, pooled SoldierViews)
├─ Spawner              (StageDirector — reads stage data, spawns gates/enemies)
├─ Pools                (soldiers, zombies, gates, VFX, damage numbers)
└─ UI                   (HUD, results, offline popup, upgrades)
```

### 11.2 Key classes
| Class | Assembly | Role |
|---|---|---|
| `RunnerSim` | `ZeroHour.Sim` | **Deterministic** stage simulation — pure logic, no Unity |
| `StageDefinition` | `ZeroHour.Sim` | Data loaded from CSV |
| `RunnerGameController` | `ZeroHour.Runner` | Orchestrates a stage run |
| `SquadController` | `ZeroHour.Runner` | Formation, movement, firing |
| `StageDirector` | `ZeroHour.Runner` | Spawns from stage data on a distance schedule |
| `GateView` / `EnemyView` / `SoldierView` | `ZeroHour.Runner` | Pooled visual representations |
| `IdleIncomeService` | `ZeroHour.Core` | Offline accrual, server-verified |
| `RunnerHud` | `ZeroHour.UI` | Count, progress, boss HP |

### 11.3 Determinism & server validation
`RunnerSim` is deterministic given `(stageId, seed, upgradeState, inputLog)`.

- The client plays the stage and sends `{ stageId, seed, inputLog, claimedResult }`
- The server **re-simulates** using the identical `Sim` code
- Mismatch → reject the reward, flag the account

This makes the runner cheat-resistant with zero extra balance logic, because the client and server literally run the same compiled method. It's also why the runner must be authored as pure logic + a thin view layer rather than as MonoBehaviours doing gameplay in `Update()`.

### 11.4 Performance budget
| Metric | Budget |
|---|---|
| Draw calls | < 60 |
| Visible soldiers | up to 400 (**GPU instanced**, one mesh) |
| Visible enemies | up to 150 (instanced) |
| Allocations per frame | **0** (full pooling) |
| Target | 60 fps on a 2020 midrange Android |

Soldiers and zombies use a single instanced mesh with per-instance colour/animation offset. **Never** one GameObject-with-Animator per soldier — 400 Animators will destroy the frame budget.

---

## 12. Data files

`tools/balance/runner_*.csv`

| File | Columns |
|---|---|
| `runner_stages.csv` | stageId, chapter, lengthUnits, bossHp, bossType, waveDensity, rewardFood, rewardIron |
| `runner_gates.csv` | stageId, atProgress, slotA_op, slotA_val, slotB_op, slotB_val |
| `runner_enemies.csv` | enemyId, hp, damage, speed, behaviour, type |
| `runner_upgrades.csv` | upgradeId, maxLevel, effectPerLevel, costBase, costGrowth |
| `runner_idle.csv` | baseRate, ratePerStage, capHours, capPerLevel |

Stages 1–20 hand-authored for a tight onboarding curve. 21–60 generated by a tuning script, then hand-reviewed.

---

## 13. Phase 1 acceptance criteria

Copied into `checklists/phase-1-mvp.md`.

- [ ] 60 stages playable start to finish
- [ ] All 7 gate operators implemented and juiced
- [ ] All 5 enemy archetypes + mid-boss + final boss
- [ ] 6 permanent upgrades with buy-max
- [ ] Idle income with the offline popup
- [ ] Save/load survives app kill
- [ ] `RunnerSim` deterministic — same inputs, same result, verified by unit test
- [ ] Every item in the §10 juice checklist ticked
- [ ] 60 fps on a real Android device
- [ ] WebGL build playable in a browser
- [ ] **3 external playtesters reach stage 20 without instruction**
- [ ] Median session ≥ 10 minutes in playtesting

---

## Next
- `07-GDD-worldmap-marches.md`
- `10-FEATURE-voice-chat.md` ★
- `11-FEATURE-arena-rooms.md` ★
- `checklists/phase-1-mvp.md` — the build list

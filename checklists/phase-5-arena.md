# Phase 5 — ★ Arena Rooms

> **Goal:** real-time, server-authoritative arena battles. The hardest phase in the project.
> **Est:** 6–8 weeks · **Docs:** `11`, `16`, `20`, `23`

**Gate to Phase 6:** 10v10 runs 5 minutes at 20 Hz with under 150 ms perceived latency on 4G · replays reproduce exactly from seed + input log

---

## 5.1 Order of work — do not skip ahead

```
1. ArenaSim, offline, local only          ← prove the rules are fun
2. 1v1 over the network                   ← prove the netcode holds
3. Prediction + reconciliation            ← prove it feels good
4. 3v3 → FFA-8 → 5v5 → 10v10 → 20v20     ← scale up only when stable
5. Normalization, ELO, ladder, AvA        ← the meta, last
```

This ordering is the mitigation for the biggest technical risk in the project (`26`). A 20v20 netcode bug is nearly impossible to diagnose; the same bug in 1v1 is tractable. Scale is the last thing added, not the first.

## 5.2 ArenaSim (`shared/ZeroHour.Sim/Arena/`)

- [ ] `ArenaState`, `ArenaSquad`, `ArenaIntent` — all fixed-point
- [ ] `ArenaSim.Tick(state, intents)` — pure, deterministic, 20 Hz step
- [ ] Movement, pathing, engagement resolution
- [ ] Hero skills with cooldowns
- [ ] Objectives: capture points, boss HP
- [ ] Win conditions per format
- [ ] **Zero-loss sparring guarantee** enforced in the sim itself, not the server layer
- [ ] Power normalization transform (`11`)
- [ ] Test: `Sparring_ChangesNoResources` — no resource delta, ever
- [ ] Test: normalization keeps the power spread within 10%
- [ ] Determinism fixtures: full matches, identical hashes across platforms

Putting the zero-loss guarantee in the sim rather than in an endpoint check means it cannot be bypassed by any future code path. It's a structural guarantee, which is what players need to trust sparring.

## 5.3 Netcode (`16`)

- [ ] 20 Hz authoritative tick loop on the server
- [ ] Intent-based client input (never state, never results)
- [ ] Delta snapshots, quantised, bandwidth-budgeted
- [ ] Client-side prediction of own squad
- [ ] Server reconciliation with smooth correction
- [ ] Entity interpolation for other players' squads
- [ ] Lag compensation within a bounded window
- [ ] Reconnect within 60 s resumes the match
- [ ] Disconnect → AI takeover; 30 s+ counts as a loss (`20 §5`)
- [ ] Server voids and refunds an in-flight match on a server restart (`24 §4`)

### Intent validation (`16 §6`, `20 §5`)
- [ ] Ownership check on every intent
- [ ] Rate limit 20 intents/s, excess dropped and counted
- [ ] Cooldowns live server-side only
- [ ] Impossible orders rejected, not clamped

## 5.4 Room lifecycle (`11`)

- [ ] Create → lobby → ready check → battle → results → cleanup
- [ ] Room list with format, map, power mode filters
- [ ] Roster locking at match start
- [ ] Host permissions, kick, disband
- [ ] Spectating
- [ ] ★ Voice auto-join per room (Phase 4 dependency)

## 5.5 Intra-alliance sparring (`11`)

- [ ] Formats: 1v1, 3v3, FFA-8, King of the Hill
- [ ] **Zero-loss guarantee** surfaced clearly in the UI
- [ ] Optional power normalization toggle
- [ ] Weekly ELO ladder + Champion title
- [ ] Diminishing ELO on repeated pairings (anti-collusion, `20 §5`)
- [ ] Reward caps per day

## 5.6 AvA (`11`)

- [ ] Formats: 5v5, 10v10, 20v20
- [ ] Challenge → accept → schedule → roster lock flow
- [ ] Modes: Deathmatch, Objective Control, Boss Race
- [ ] Bracket tournament, 8 or 16 alliances
- [ ] War points, leaderboard, winner buff
- [ ] Forfeit penalties, cooldowns

## 5.7 Replays & audit

- [ ] Server records seed + input log per match
- [ ] Replay reproduces the match exactly
- [ ] Replays are **server-generated only**; the client never uploads one (`20 §5`)
- [ ] Replay viewer with scrub and speed control

## 5.8 Client presentation

- [ ] `Arena.unity`, additive
- [ ] GPU instancing for units — **required** for 20v20 (`18 §3`)
- [ ] Team colour rims, distinct silhouettes, readable at 32 px
- [ ] HUD per `11 §7.2`: skills bottom-right, PTT bottom-left
- [ ] Kill feed, objective status, timer
- [ ] Victory/defeat sequence
- [ ] Low-tier quality path: visual unit cap 20/side

## 5.9 Network condition testing (`23 §6`) — mandatory

- [ ] Signed off at 50, 150, 300, 500 ms RTT
- [ ] Signed off at 1%, 5%, 10% packet loss
- [ ] ±100 ms jitter
- [ ] Drop/restore at 20 points in a match
- [ ] Cellular ↔ WiFi handover mid-match on a physical device

---

## Gate checklist

- [ ] 10v10, 5 minutes, 20 Hz, under 150 ms perceived latency on real 4G
- [ ] 20v20 holds 60 fps mid-tier, 30 fps low-tier
- [ ] Server tick overrun under 1% with 10 concurrent 20v20 matches
- [ ] Replays reproduce exactly from seed + input log
- [ ] Sparring provably costs nothing — verified in the DB, not just the UI
- [ ] **An HQ 9 player beats an HQ 24 player in normalized mode through better play**
- [ ] **After five sparring matches, would you play a sixth?** Answer honestly

The normalization test and the sixth-match question are the whole point of the feature. If normalized mode still lets power win, the mode is decoration. If you wouldn't play a sixth match, the format needs work before the meta gets built on top of it.

→ Next: [phase-6-liveops-events.md](phase-6-liveops-events.md)

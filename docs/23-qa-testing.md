# 23 — QA & Testing Strategy

> One developer, no QA team. Automation is not a luxury here — it's the only way to move fast without breaking things.

---

## 1. The testing pyramid, adapted

```
        ▲  Manual playtesting        (weekly, device matrix)
       ╱ ╲ Play-mode tests           (scenes, UI flows, ~30)
      ╱   ╲ Integration tests        (API + DB, ~100)
     ╱     ╲ Determinism fixtures    (1,000 cases — the safety net)
    ╱_______╲ Unit tests             (Sim rules, ~500, sub-second)
```

**Most of the value is at the bottom.** `shared/ZeroHour.Sim` has no Unity dependency, so its entire test suite runs in under a second in plain .NET. That speed is what makes test-driven development on game rules actually practical.

---

## 2. Unit tests — the Sim

Everything in `shared/ZeroHour.Sim` is a pure function, which makes it trivially testable.

```csharp
[Fact] public void BuildTime_IsMonotonic() { ... }
[Fact] public void Accrual_ClampsAtCapacity() { ... }
[Fact] public void CounterTriangle_IsSymmetric() {
    Assert.Equal(Fixed.One, Combat.Mult(Tank, Air) * Combat.Mult(Air, Tank));
}
[Fact] public void Battle_WoundedRule_NeverExceeds30Percent() { ... }
[Fact] public void Gacha_PityAlwaysFiresBy60() { ... }
[Fact] public void Sparring_ChangesNoResources() { ... }        // ★ zero-loss
[Fact] public void Normalization_KeepsSpreadWithin10Percent() { ... }  // ★
```

**Coverage target: 90%+ on `Sim`.** Not on the whole codebase — chasing coverage on view code is wasted effort. The sim is where correctness matters, because it decides what players own.

---

## 3. Determinism fixtures — the critical test

```
tools/fixtures/*.json      1,000 recorded scenarios
   { seed, initialState, inputs[], expectedFinalHash }
```

Run the same fixtures on:
- Windows x64 (dev machine)
- Android ARM64 (device farm / physical device)
- Linux ARM64 (the production server)

**All three must produce byte-identical final state hashes.** Any divergence is a P0 bug — it means a float leaked into the sim, a dictionary is being enumerated, or a platform-specific maths path exists. Everything else in the project rests on this holding.

This suite covers: runner stages, battle resolution, ★ arena matches, and economy accrual over long timespans.

---

## 4. Integration tests

Real HTTP against a real Postgres and Redis (Testcontainers).

| Area | Cases |
|---|---|
| Auth | Guest create, upgrade, refresh rotation, reuse detection |
| Base | Upgrade with/without resources, queue limits, help acceleration |
| World | March validation, arrival resolution, shield rules, teleports |
| Alliance | Join/leave/kick/rank permissions, help dedupe, gift claims |
| Events | Score triggers, milestone claims, idempotency |
| ★ Arena | Room lifecycle, roster locking, intent rejection, ELO update |
| ★ Voice | Token minting, membership checks, ejection on kick |
| Purchases | Valid receipt, invalid receipt, replayed receipt, refund |

**Idempotency is tested explicitly:** every mutating endpoint is called twice with the same request id and must produce exactly one effect. Mobile networks retry; a non-idempotent grant endpoint is a duplication exploit waiting to happen.

---

## 5. Play-mode tests (Unity)

Kept deliberately few — they're slow and brittle.

- Boot → Main loads without exceptions
- Scene transitions in both directions for each layer
- Tutorial completes end-to-end
- Purchase flow reaches the store (mocked)
- ★ Voice widget survives a scene change
- ★ Arena scene loads, renders 40 units, and stays above 30 fps

---

## 6. Network condition testing (★ arena, mandatory)

| Condition | Tool |
|---|---|
| 50 / 150 / 300 / 500 ms RTT | `clumsy` (Win), `tc netem` (Linux) |
| 1% / 5% / 10% packet loss | Same |
| ±100 ms jitter | Same |
| Connection drop and restore | Manual toggle at 20 points in a match |
| Cellular ↔ WiFi handover | Physical device |

**Testing only on a fast, stable connection is how shipped netcode fails.** Every arena feature is signed off under 300 ms + 5% loss before it's considered done.

---

## 7. Device matrix

| Tier | Device | Purpose |
|---|---|---|
| Low | 2–3 GB RAM, Android 8–10 | The performance floor |
| Mid | 4–6 GB, Android 12 | The bulk of the audience |
| High | 8 GB+, Android 14+ | Best-case |
| Tablet | Any | Layout sanity |

Test on **at least one physical low-end device**. Emulators lie about thermal behaviour, battery, and real GPU limits — which are exactly the things that break ★ voice and ★ arena.

---

## 8. Performance testing

| Test | Pass condition |
|---|---|
| Cold start | < 5 s to playable |
| Memory over 30 min | No upward trend (leak check) |
| ★ 20v20 arena | 60 fps mid-tier, 30 fps low-tier |
| ★ Voice + gameplay | < 6%/h battery, no frame drops |
| Draw calls | Within `17 §5` budgets |
| GC | 0 B/frame in steady state |
| Server load | 10 concurrent 20v20 within CPU budget, tick overrun < 1% |

---

## 9. Manual playtest protocol

Weekly, and at every phase gate:

```
1. Fresh install, no data          → is the first 60 seconds good?
2. Complete the tutorial           → any confusion, any dead end?
3. Play 20 minutes as a new player → is there always a next thing to do?
4. Load a mid-game save            → does progression still feel fair?
5. ★ Play 5 sparring matches       → is it fun? Would you play a 6th?
6. ★ Join a voice channel          → does it just work?
7. Try to break it                 → spam taps, rotate, background, airplane mode
```

**Question 5 is the honest one.** "Would I play another?" is the only real measure of the arena feature, and it's worth answering truthfully even when the answer is inconvenient.

---

## 10. CI pipeline (GitHub Actions)

```yaml
on: [push, pull_request]
jobs:
  sim:        # < 1 min — runs on every push
    - dotnet test shared/ZeroHour.Sim.Tests
    - dotnet test --filter Category=Determinism
    - balance validation (12 §7)
  server:     # ~3 min
    - dotnet test server/ZeroHour.Tests   (Testcontainers)
    - dotnet list package --vulnerable
    - secret scan
  unity:      # ~15 min, nightly + on release branches
    - edit-mode + play-mode tests
    - Android build
    - upload artifact
```

**The sim job runs on every push and must stay under a minute.** Fast feedback on the layer that matters most is worth optimising for.

---

## 11. Bug triage

| Severity | Definition | Response |
|---|---|---|
| **P0** | Data loss, exploit, crash on launch, determinism divergence | Stop everything |
| **P1** | Core loop broken, purchase failure, ★ arena unplayable | Same day |
| **P2** | Feature broken with a workaround | Same week |
| **P3** | Cosmetic, minor UX | Backlog |

**Every P0 and P1 gets a regression test before the fix is merged.** Otherwise the same bug returns in three months and you get to debug it twice.

---

## Next
- `24-devops-deployment.md`
- `16-netcode-realtime.md §10`

# Phase 1 — MVP: Runner + Idle

> **Goal:** a complete, shippable, genuinely fun game. This is the acquisition hook and it must stand alone.
> **Est:** 4–6 weeks · **Docs:** `06`, `03`, `12`, `19`

**Gate to Phase 2:** the runner is fun for 10+ minutes with no changes · WebGL build playable in a browser · 3 external playtesters completed 10 stages · the D1 idle claim survives an app restart

---

## 1.1 Sim first (`shared/ZeroHour.Sim/Runner/`)

All of this is written and tested **without Unity** (`28 §3`).

- [~] `RunnerState` — squad count, unit tier, weapon level, HP, position, distance. Tick, squad, X and distance are in and fingerprinted; HP arrives with combat

- [x] `RunnerInput` — lane position as `Fixed`, per tick; clamped to the corridor at construction
- [x] `RunnerSim.Step(state, input)` — pure, deterministic, fixed-point. **Signature deviates from the plan: no `dt`.** A caller-supplied delta is the standard way a deterministic sim stops being deterministic, since a client stepping at frame time and a server stepping at a fixed rate accumulate different rounding and honest players then fail reward validation. Tick length is fixed at 20 Hz (`RunnerTuning.TicksPerSecond`) and the view layer interpolates for rendering

- [ ] `RunnerSim.Simulate(stageDef, inputLog, seed, stats)` → full result (for server re-validation, `20 §4`)
- [x] Gate resolution: `+N`, `×N`, `−N`, `÷N`, weapon upgrade, type swap — `Runner/Gate.cs`, all 7 operators incl. `♥ Shield`; malformed gates throw at construction
- [ ] Auto-fire: rate, damage, range derived from squad + weapon
- [ ] Enemy waves from a stage definition
- [ ] Mid-boss and HP-bar wall boss
- [ ] Collision and pickup resolution
- [ ] Score and reward computation
- [ ] `IdleSim.Accrue(highestStage, elapsedMs, caps)` with the overflow cap (`12`)

### Tests
- [ ] Same seed + same inputs ⇒ identical final state hash, 100 runs
- [x] Gate math never produces a negative or zero squad below 1 — clamped in `Squad.MinCount`; a gate is a choice, so only combat may end a run
- [x] `×N` then `÷N` returns the original count — holds for integer factors on any count. **Caveat:** ×/÷ both floor, so mismatched factors drop the remainder (×3 then ÷2 on 7 → 10, not 10.5). Asserted both ways in `GateTests` rather than left latent; fractional soldiers cannot be drawn
- [ ] Idle accrual clamps at capacity and never goes backwards
- [ ] A full 20-stage playthrough simulates in under 50 ms

## 1.2 Runner gameplay (Unity)

- [ ] `Runner.unity` scene, additive from `Main`
- [ ] Camera rig, fixed follow, portrait framing
- [ ] Drag-to-steer input, `Input System`, thumb-zone friendly (`19 §1`)
- [ ] Squad view: pooled unit instances, formation layout, GPU instancing
- [ ] View layer reads `RunnerState` and never mutates it
- [ ] Gate prefabs with clear +/×/−/÷ labelling, readable at a glance
- [ ] Enemy spawning from the sim, pooled
- [ ] Projectiles pooled, no per-shot allocation
- [ ] Boss encounter with an HP bar
- [ ] Stage clear / fail flow
- [ ] Fixed-rate sim step decoupled from render (`17 §5`)

## 1.3 Progression & idle

- [ ] 20 stages authored in `data/runner_stages.csv`
- [ ] Difficulty curve tuned per `06`
- [ ] Stage select screen
- [ ] Idle income tied to highest cleared stage
- [ ] Offline earnings popup on return, with the accrual explained
- [ ] Upgrade screen: squad size, damage, fire rate — costs from data
- [ ] Local save via `ISaveService`, schema-versioned for server migration later
- [ ] Save survives app kill and restart

## 1.4 UI (`19`)

- [ ] HUD: minimal — HP and pause only, per `19 §8`
- [ ] Stage select
- [ ] Results screen with reward reveal
- [ ] Offline-earnings popup
- [ ] Upgrade screen
- [ ] Settings: audio sliders, reduced motion, quality override
- [ ] All buttons ≥ 44×44 dp, actions in the bottom third
- [ ] TextMeshPro throughout, canvases split static/dynamic

## 1.5 Juice pass (`19 §4`)

This is the difference between "works" and "fun." Budget real time for it.

- [ ] Hit feedback: flash, knockback, impact SFX
- [ ] Damage numbers, pooled
- [ ] Screen shake, capped and disableable
- [ ] Gate pass: satisfying pop, count roll-up
- [ ] Boss death: slow-mo, particles, fanfare
- [ ] Haptics on key beats
- [ ] Reward reveal staging on the results screen
- [ ] Music loop + SFX family, mixer with ducking

## 1.6 Analytics (`21`)

Instrument now, not in Phase 9.

- [ ] `app_opened`, `session_started`, `session_ended`
- [ ] `runner_stage` with result, duration, retries
- [ ] `tutorial_step`
- [ ] `currency_earned` / `_spent`
- [ ] Consent prompt before any send
- [ ] Events batched, queued offline

## 1.7 Build & playtest

- [ ] `Tools → Zero Hour → Build WebGL` works
- [ ] WebGL hosted free (GitHub Pages / Netlify)
- [ ] Android dev build installs and runs on a physical device
- [ ] 60 fps on mid-tier, 30 fps floor on low-tier (`23 §8`)
- [ ] Zero GC allocation per frame in steady state — verified in the profiler
- [ ] Cold start to playable under 5 s

---

## Gate checklist — the most important gate in the project

- [ ] **Five people who don't know you play it.** Watch them, say nothing
- [ ] Do they play a second stage without being asked?
- [ ] Do 3+ of them reach stage 10?
- [ ] Do they come back the next day, unprompted, for the idle claim?
- [ ] Would *you* play this on a bus?

**If the answer to the first question is no, stay in Phase 1.** Retune the gate spacing, the fire rate, the difficulty curve, the juice. Ship a new WebGL link and test again.

Everything in Phases 2–10 assumes this loop is fun. A base builder bolted onto a boring runner is a worse product than a good runner alone — and the runner alone is genuinely shippable, which is the point of ordering the roadmap this way (`26`).

→ Next: [phase-2-vertical-slice.md](phase-2-vertical-slice.md)

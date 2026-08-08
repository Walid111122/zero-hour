# Phase 8 — Polish, Optimization & Localisation

> **Goal:** make it feel like a real product on a real, cheap phone.
> **Est:** 5–6 weeks · **Docs:** `18`, `19`, `17`, `23`

**Gate to Phase 9:** 60 fps sustained on a 4-year-old midrange Android · memory under 220 MB · download under 150 MB

---

## 8.1 Performance (`17 §5`, `23 §8`)

- [ ] Profiler pass on every scene; document the baseline
- [ ] Draw calls within budget: <100 base/world, <150 ★ arena
- [ ] **Zero GC allocation per frame** in steady state — verified, not assumed
- [ ] Object pooling: units, projectiles, VFX, damage numbers, UI list items
- [ ] GPU instancing on all repeated units
- [ ] Sprite atlases per screen group
- [ ] Canvas splitting: static vs dynamic (`19 §7`)
- [ ] Recycling scroll views on every long list
- [ ] Addressables + on-demand download for world/arena/heroes/events
- [ ] Memory under 220 MB; no upward trend over 30 minutes
- [ ] AAB under 150 MB
- [ ] Cold start under 5 s to playable
- [ ] Three quality tiers verified on real devices in each class
- [ ] Thermal check: 30 minutes of play without throttling to unplayable

## 8.2 Art pass (`18`)

Now that mechanics are final, art is safe to make (`18 §4`).

- [ ] Consistent CC0/self-made style across all units and buildings
- [ ] Silhouette readability verified at 32 px for every troop type
- [ ] ★ Arena team colour rims and friend/foe clarity
- [ ] Baked lighting, single directional light
- [ ] VFX pass within the particle budget
- [ ] UI art: 9-slice panels, icon set, consistent spacing
- [ ] `docs/ATTRIBUTION.md` complete and accurate for every third-party asset

## 8.3 Audio (`18 §5`)

- [ ] Full SFX coverage: UI, runner, base, combat, ★ arena
- [ ] Music: menu, base, world, ★ arena, victory/defeat
- [ ] Mixer with ★ voice ducking (music −12 dB, SFX −6 dB)
- [ ] Separate sliders: Master / Music / SFX / Voice
- [ ] OS silent switch respected
- [ ] Audio memory under 30 MB
- [ ] No audio in the first 2 s of cold start

## 8.4 Juice (`19 §4`)

- [ ] Every reward moment has visual + audio + haptic feedback within 100 ms
- [ ] Level up, chest open, gacha reveal, ★ arena victory all staged
- [ ] Screen shake capped and disableable
- [ ] Transitions and loading veils smooth, never a black frame

## 8.5 Localisation (`18 §6`)

- [ ] 8 languages: EN, AR, ES, PT, TR, RU, ID, ZH
- [ ] Noto Sans font family covering all scripts
- [ ] **Arabic RTL: full layout mirroring**, not just text direction
- [ ] Directional icons mirrored
- [ ] Every UI tested at +40% text length
- [ ] Locale-aware number formatting
- [ ] Missing key → English fallback + log, never a raw key on screen
- [ ] Reviewed by a native Arabic reader

Arabic RTL is engineering work, not translation work, and it's the most likely place for a layout to break badly. Given the target markets (`02 §4`), it's worth doing properly rather than shipping a mirrored-but-broken UI.

## 8.6 Accessibility (`19 §6`)

- [ ] Colourblind palettes; **no state conveyed by colour alone**
- [ ] Text scaling 100/125/150%
- [ ] Reduced motion mode
- [ ] Haptics toggle
- [ ] Minimum 44×44 dp touch targets, verified
- [ ] WCAG AA contrast on all text
- [ ] ★ Voice ↔ text chat parity confirmed

## 8.7 FTUE rebuild (`19 §5`)

- [ ] Rebuild onboarding using everything learned since Phase 2
- [ ] Data-driven steps, tunable without a build
- [ ] Playable within 10 s of cold start
- [ ] One objective on screen at a time
- [ ] Analytics funnel per step, drop-off visible

## 8.8 Stability

- [ ] Sentry integrated on client and server
- [ ] Crash-free sessions ≥ 99.5%
- [ ] Every P0/P1 from the phase has a regression test (`23 §11`)
- [ ] Airplane-mode, backgrounding, rotation, and interrupt handling tested
- [ ] Memory leak check across 30 minutes of mixed play

---

## Gate checklist

- [ ] 60 fps sustained on a 4-year-old midrange Android
- [ ] Memory under 220 MB
- [ ] AAB under 150 MB
- [ ] Zero per-frame allocation confirmed in the profiler
- [ ] Arabic RTL reviewed by a native reader
- [ ] Crash-free ≥ 99.5% across the test matrix
- [ ] Attribution file complete — a licence audit would pass

→ Next: [phase-9-softlaunch.md](phase-9-softlaunch.md)

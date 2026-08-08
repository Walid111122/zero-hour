# Phase 9 — Security Hardening & Soft Launch

> **Goal:** try to break your own game, then put it in front of real strangers in a cheap market.
> **Est:** 4–6 weeks · **Docs:** `20`, `22`, `24`, `25`

**Gate to Phase 10:** D1 ≥ 35% · D7 ≥ 15% · crash-free sessions ≥ 99.5% · zero P0 bugs open

---

## 9.1 Security audit (`20`)

- [ ] Every mutating endpoint reviewed: does it read any client-computed result? Fix if yes
- [ ] 100% of runner submissions re-simulated server-side (`20 §4`)
- [ ] ★ Arena intent validation complete (`16 §6`)
- [ ] Rate limits on every endpoint, per-IP and per-player
- [ ] Refresh-token rotation with reuse detection verified
- [ ] Parameterised queries everywhere — no string-concatenated SQL anywhere
- [ ] Secrets absent from the repo; secret scanner green
- [ ] Dependencies pinned; `--vulnerable` scan clean
- [ ] Admin panel: TOTP, IP allowlist, no public exposure, audit log
- [ ] TLS 1.3 only, HSTS, cert pinning in the client
- [ ] Bot-detection signals collected and scored (`20 §6`)
- [ ] Feature flags exist for every major system

### Penetration self-test (`20 §1`)
Work through the threat table and document each attempt and outcome:
- [ ] Memory-edit resources with a tool — confirm nothing real changes
- [ ] Forge a runner score — confirm rejection
- [ ] Replay a packet — confirm idempotency holds
- [ ] Send an intent for another player's ★ arena squad — confirm rejection
- [ ] Forge a purchase receipt — confirm rejection
- [ ] Bypass a ★ voice mute client-side — confirm the SFU still enforces it
- [ ] Spoof the device clock — confirm timers unaffected
- [ ] Write up results in `docs/security-audit-phase9.md`

## 9.2 Legal & store readiness (`22`)

- [ ] Privacy Policy — **lawyer reviewed** ⚠️
- [ ] Terms of Service — **lawyer reviewed** ⚠️
- [ ] Community Guidelines published
- [ ] Attribution page complete
- [ ] Gacha rates visible at the point of pull
- [ ] Age gate tested, including the under-13 ★ voice block
- [ ] Account deletion works in-app **and** via a web URL
- [ ] Data export works
- [ ] ★ Voice clip 7-day auto-deletion verified in the real environment
- [ ] Play Data Safety form matches actual behaviour, including microphone
- [ ] Content rating completed with UGC declared
- [ ] DPAs signed with every third-party processor
- [ ] Breach response process written

## 9.3 Infrastructure readiness (`24`)

- [ ] Production VM hardened per `24 §2`
- [ ] Backups running nightly, encrypted, off-VM
- [ ] **Restore drill completed and documented** — restore, migrate, boot, load a player
- [ ] Monitoring and alerting live (`21 §6`)
- [ ] Runbooks written (`24 §10`)
- [ ] Load test: 1,000 concurrent simulated players
- [ ] Load test: 10 concurrent ★ 20v20 arena matches, tick overrun under 1%
- [ ] Staged rollout and rollback path tested on a throwaway release

## 9.4 Store listing (`25 §2`)

- [ ] Google Play developer account ($25 — the only unavoidable cost)
- [ ] Icon, 8 screenshots, feature graphic, 30 s video of real gameplay
- [ ] Listing localised into all 8 languages
- [ ] ASO keyword pass
- [ ] Upload keystore created and **backed up in two encrypted locations** (`24 §8`)

## 9.5 Soft launch ladder (`25 §3`)

- [ ] Closed alpha — 20 testers (friends, Discord)
      Gate: crash-free > 99%, tutorial completion > 70%
- [ ] Closed beta — 200 testers (PH, VN, ID)
      Gate: D1 > 35%, D7 > 15%, ★ voice > 25%, ★ sparring > 35%
- [ ] Open beta — 5,000+ (+ BR, TR, EG, MX)
      Gate: D1 > 40%, D7 > 20%, conversion > 1.5%, ARPDAU > $0.10

## 9.6 Tuning from real data

- [ ] Economy retune from actual faucet/sink telemetry (`12`)
- [ ] FTUE iteration from funnel drop-off (data-driven, no client build)
- [ ] ★ Arena format popularity → promote what people play
- [ ] Difficulty curve adjustment from stage completion rates
- [ ] Offer performance review; cut anything that underperforms or feels pushy
- [ ] Bug triage to zero P0 and zero P1

---

## Gate checklist

- [ ] Security self-test documented, no unresolved findings
- [ ] Legal documents live and lawyer-reviewed
- [ ] Restore drill proven
- [ ] Load tests pass
- [ ] **D1 ≥ 35%, D7 ≥ 15%**
- [ ] Crash-free ≥ 99.5%
- [ ] Zero P0/P1 open

**If retention fails the gate, do not launch globally.** Fix and retest. The organic discovery window opens once, and the store's ranking will remember a weak launch (`25 §3`).

→ Next: [phase-10-launch-live.md](phase-10-launch-live.md)

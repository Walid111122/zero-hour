# Phase 7 — Monetization

> **Goal:** purchases that work correctly and feel fair.
> **Est:** 4–5 weeks · **Docs:** `13`, `12`, `21`, `22`

**Gate to Phase 8:** receipt validation rejects a forged receipt · the analytics funnel shows install → tutorial → first purchase

---

## 7.1 Store integration (`13`)

- [ ] Google Play Billing v7 via Unity IAP
- [ ] Products defined in Play Console, mirrored in `data/iap_products.csv`
- [ ] Purchase flow: initiate → store → receipt → **server validation** → grant
- [ ] **Server-side receipt validation against Google** before any grant (`20 §2`)
- [ ] Replayed receipt rejected (idempotency by transaction id)
- [ ] Forged receipt rejected — tested explicitly
- [ ] Restore purchases after reinstall
- [ ] Refund handling: revoke granted currency where feasible
- [ ] Pending/deferred purchase states handled
- [ ] Every grant writes a `currency_audit` row (`15 §7`)

## 7.2 Offer catalogue (`13`)

- [ ] Starter pack (one-time, first session)
- [ ] Growth fund (pay once, unlock over progression)
- [ ] Monthly card / privilege cards
- [ ] Season pass premium track (Phase 6 dependency)
- [ ] VIP ladder 1–20
- [ ] Diamond bundles
- [ ] Resource bundles
- [ ] Hero-specific bundles
- [ ] Speedup bundles
- [ ] ★ Arena cosmetic bundles (no competitive advantage — sparring is normalized)
- [ ] State-triggered contextual offers

## 7.3 The fairness rules (`02`, `13`)

Non-negotiable, and each needs a test or a documented check:

- [ ] **Every paid path has a documented free equivalent** — slower, never blocked
- [ ] Gacha rates published in-app at the point of pull (`22`)
- [ ] No pay-to-win in ★ normalized sparring — power is equalised by design
- [ ] No red dot ever appears for a paid action (`19 §3`)
- [ ] No countdown pressure on offers beyond a genuine event window
- [ ] One-tap dismiss on every offer popup
- [ ] Purchases never gate core progression
- [ ] VIP benefits documented alongside their free alternatives

If an offer feels manipulative during review, cut it. The fairness pillar is a differentiator in this genre (`25 §1`), and it only works if it's real.

## 7.4 Alliance gift multiplier (`08`, `13`)

- [ ] A purchase triggers gifts for the whole alliance
- [ ] Scales with spend tier
- [ ] Makes a whale's spend socially positive rather than resented

## 7.5 Analytics & KPIs (`21`)

- [ ] `iap_offer_shown` with trigger and context
- [ ] `iap_purchase_started` / `_completed` / `_failed`
- [ ] `ad_rewarded_completed`
- [ ] Purchase funnel dashboard
- [ ] Conversion, ARPPU, ARPDAU, offer performance
- [ ] Alert on purchase validation failure rate above 1%

## 7.6 Rewarded ads

- [ ] Placements: extra runner attempt, speedup boost, double idle claim
- [ ] Capped per day, never required for progression
- [ ] Declared in the Play listing (`22 §4`)
- [ ] Fully skippable experience for players who never watch one

---

## Gate checklist

- [ ] Real purchase with a real card grants correctly
- [ ] Forged receipt rejected — verified by attempting it
- [ ] Replayed receipt grants exactly once
- [ ] Restore after reinstall works
- [ ] Every grant has an audit row
- [ ] Funnel visible: install → tutorial → first purchase
- [ ] Fairness review: read every offer and ask whether it feels predatory
- [ ] Free-path documentation exists for every paid benefit

→ Next: [phase-8-polish-optimization.md](phase-8-polish-optimization.md)

# 13 — Monetization & IAP

> We sell **time and convenience**, never exclusive power.
> Phase 8. Nothing monetizable ships before the game is worth playing.

---

## 1. The offer ladder

Ordered by the sequence a player encounters them.

| # | Offer | Price | Function |
|---|---|---|---|
| 1 | **Starter Pack** | $0.99 | Breaks the payment barrier. Deliberately absurd value. Once per account |
| 2 | **Growth Fund** | $4.99 | Pay once, receive drip rewards as you hit HQ milestones → a return obligation |
| 3 | **Monthly Card (Basic)** | $4.99 | 100💎/day for 30 days → a daily login contract |
| 4 | **Monthly Card (Premium)** | $9.99 | 250💎/day + build queue 2 + speed buffs |
| 5 | **Season Pass** | $9.99 | 8-week free + paid tracks |
| 6 | **Diamond packs** | $0.99 – $99.99 | 6 tiers, bonus % rises with size |
| 7 | **VIP ladder** | Cumulative spend | Permanent stacking buffs, 20 levels |
| 8 | **Hero banners** | 300💎/pull | The deep well |
| 9 | **Resource / speedup bundles** | $4.99 – $99.99 | Whale throughput |
| 10 | **Contextual offers** | $1.99 – $49.99 | Triggered by game state |
| 11 | **★ Arena cosmetics** | $1.99 – $9.99 | Frames, banners, victory effects — **zero power** |

### Why #11 matters
Arena cosmetics are the only pure-cosmetic revenue in the plan, and they exist because ★ sparring gives skilled F2P players status. Status seeks expression. Selling them a way to display it monetizes the fairness feature without compromising it.

---

## 2. VIP ladder

```
VIP level from cumulative VIP points (1 point per $0.01 spent, plus event grants)
VIP 1  →  +2% construction speed
VIP 5  →  +5% all speeds, +1 daily free speedup
VIP 6  →  BUILD QUEUE 2                    ← also free via tech (04 §5)
VIP 10 →  +1 march queue, +10% gather
VIP 12 →  build queue 3
VIP 15 →  +15% construction, bigger offline cap
VIP 20 →  max buffs, exclusive frame
```

**Every VIP benefit has a free equivalent path** — slower, but real. Build queue 2 is the clearest example: VIP 6 or a tech node. This is Pillar 4 made concrete, and it's what keeps the game from being pay-to-win.

---

## 3. Contextual offers

Triggered by real game state, with strict rules.

| Trigger | Offer |
|---|---|
| Just got attacked | Shield + resource bundle |
| Stalled on the same runner stage 3× | Upgrade bundle |
| HQ upgrade needs 6+ more hours | Speedup bundle |
| Hospital full | Healing bundle |
| 1 pull from pity | Pull bundle |
| ★ Lost 3 AvA matches | Troop training bundle |

### Rules (non-negotiable)
- **Max 1 contextual offer per session**, max 3/day
- Never interrupt gameplay — the offer waits for a natural screen boundary
- Always dismissible in one tap, never a fake close button
- **Never manufacture panic.** "You're about to lose everything" is banned copy. The trigger describes a real situation; the offer is a real solution.

The distinction matters. A shield offer after an attack is genuinely useful. A shield offer with a countdown claiming your base is doomed is manipulation, and it earns short-term revenue at the cost of trust, reviews, and retention.

---

## 4. What we will not do

| Practice | Why not |
|---|---|
| Loot boxes with hidden rates | Publish rates. Legally required in several markets, and it builds trust |
| Purchase-exclusive power | Breaks Pillar 4 |
| Energy walls that hard-block play | Stamina limits zombie hunting only, never the runner or ★ arena |
| Fake countdown timers | Fraudulent |
| Manufactured-panic offers | Predatory |
| Ads that misrepresent the game | Our runner *is* the game |
| Pay-to-skip-the-only-content | If the game isn't fun unpaid, fix the game |
| Removing free paths to add paid ones | Every paid path keeps its free twin |

---

## 5. Ads (Phase 8, rewarded only)

**Rewarded video only. No interstitials, no banners.**

| Placement | Reward | Cap/day |
|---|---|---|
| Offline income ×2 | Double the collected amount | 3 |
| Runner retry boost | +50% damage for one attempt | 5 |
| Free speedup | 30 min | 3 |
| Lucky wheel extra spin | 1 spin | 2 |
| Gacha discount | −20% on next pull | 1 |

Mediation: **AdMob** (free, highest fill). Ads are opt-in value, never a tax on playing. A player who never watches an ad should never feel punished.

---

## 6. Store & compliance

- **Google Play Billing v7** (required), server-side receipt validation, **always**
- Never grant entitlement on a client claim — validate the receipt server-side first
- Restore purchases on reinstall (non-consumables + active subscriptions)
- Localised pricing via Play's automatic conversion
- Subscription rules: clear renewal terms, one-tap cancel path, Play's required disclosures
- Refund handling: revoke granted currency where feasible, log all revocations
- **Published gacha rates in-app** (see `05 §4.4` and `22`)
- Spending limits: optional self-imposed monthly cap in settings (good practice, and pre-empts regulation)

---

## 7. Revenue model ⚠️ (targets, not promises)

```
Conversion (ever paid)      : 2 – 3%
ARPPU                       : $25 – 40/month
ARPDAU                      : $0.15 – 0.30
Revenue from top 0.1%       : 55 – 65%
```

At 10,000 DAU and $0.20 ARPDAU: **$2,000/day ≈ $60k/month**. That is the point at which the infrastructure stops being free-tier and starts being a line item you can happily afford.

### The honest framing
The 97% who never pay are not failures — they are the population that makes domination meaningful for the 3% who do. Every F2P-retention mechanic in this project (wounded rule, warehouse protection, shields, alliance gifts, ★ zero-loss sparring, ★ power normalization) is also a revenue mechanic, because whales need an audience.

---

## 8. Phase 8 acceptance criteria
- [ ] Play Billing v7 integrated with **server-side receipt validation**
- [ ] All 11 offer types implemented
- [ ] VIP ladder with every benefit having a documented free path
- [ ] Contextual offers respecting all §3 rules
- [ ] Rewarded ads with daily caps, no interstitials
- [ ] Restore purchases works after reinstall
- [ ] **Alliance Gifts fire on every purchase** (`08 §4`)
- [ ] Gacha rates displayed in the recruitment UI
- [ ] Self-imposed spending cap available in settings
- [ ] Purchase → entitlement → audit log verified end-to-end
- [ ] No offer can be triggered mid-gameplay

---

## Next
- `14-tech-architecture.md`
- `22-legal-compliance.md`

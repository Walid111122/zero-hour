# Phase 10 — Global Launch & Live Operations

> **Goal:** launch, then run the game as a service without burning out.
> **Est:** ongoing · **Docs:** `25`, `24`, `21`, `09`

Entry condition: Phase 9's retention gate passed. If it didn't, you're still in Phase 9.

---

## 10.1 Launch week (`25 §5`)

- [ ] −7d: store listing live, pre-registration open, Discord announcement
- [ ] −3d: load test at 3× expected peak; backups and restore drill re-verified
- [ ] −1d: feature flags reviewed, runbooks re-read, support inbox ready
- [ ] **A hotfix build ready to ship, not just ready to write**
- [ ] Day 0: staged rollout 5%, crash rate monitored hourly
- [ ] Day 1: 20% if crash-free > 99.5%
- [ ] Day 2: 50%, first live event, first ★ arena tournament
- [ ] Day 3: 100%, publish the launch devlog
- [ ] Day 7: retention review, first balance pass, written retro

## 10.2 Operating cadence (`25 §6`)

| Cadence | Work |
|---|---|
| Daily | Alerts, support replies, Discord, moderation queue |
| Weekly | Live event, ★ arena tournament, patch notes, one balance tweak |
| Bi-weekly | Community poll on what to build next |
| Monthly | New hero, new ★ arena map, devlog, ASO iteration |
| Quarterly | Season, major feature |

- [ ] Cadence documented and actually sustainable at one person's capacity
- [ ] Most weekly work doable **server-side, no client build** (`24 §9`)

## 10.3 Community

- [ ] Discord live: recruitment, bug reports, dev updates, tournaments
- [ ] **Respond to every store review**, including the 1-stars
- [ ] Support inbox with a target 48 h first response
- [ ] Published moderation stats (DSA obligation, `22 §3`)
- [ ] Transparent patch notes, including nerfs and the reasoning

## 10.4 Live-ops health

- [ ] Weekly KPI review against `21 §1` targets
- [ ] Economy monitoring: faucet/sink balance, inflation watch (`12`)
- [ ] ★ Voice adoption and ★ sparring participation tracked as first-class metrics
- [ ] A/B testing programme started (`21 §7`) — FTUE first
- [ ] Cost checkpoints monitored (`24 §11`)
- [ ] Incident response practised, not just documented

## 10.5 Scaling triggers (`14 §9`, `24 §11`)

- [ ] CPU > 70% sustained → move Postgres to its own VM
- [ ] Egress > 7 TB/month → Cloudflare asset caching, review ★ voice bitrate
- [ ] DAU > 5,000 → execute the documented scale-out path
- [ ] Revenue > $1,000/month → **stop optimising for free tiers and buy proper infrastructure**

## 10.6 Post-launch roadmap candidates

Ordered by expected value, not by what's most fun to build:

1. More ★ arena maps and formats (the differentiator; cheap to extend)
2. Cross-alliance sparring with matchmaking
3. New heroes on a monthly cadence
4. Seasons with meaningful meta shifts
5. iOS port (once Android economics are proven)
6. Cross-server events
7. Spectator tournaments with prize support

- [ ] Roadmap published and revisited quarterly against real data
- [ ] Nothing added because a competitor has it; only because our metrics ask for it

## 10.7 Sustainability

- [ ] Weekly time budget capped; live-ops is a marathon (`26` risk R7)
- [ ] Automate anything done more than three times
- [ ] Backups and monitoring reliable enough to take a week off
- [ ] Honest quarterly review: is this still worth continuing?

That last box matters. A game that earns a modest, steady income and takes ten hours a week is a success. One that earns the same and takes sixty is not. Build toward the first.

---

## Success review (6 months post-launch, `25 §7`)

| Metric | Minimum | Good | Actual |
|---|---|---|---|
| Installs | 50,000 | 250,000 | |
| DAU | 2,000 | 10,000 | |
| D1 / D7 / D30 | 35/15/8% | 45/25/12% | |
| ★ Voice weekly | 25% | 45% | |
| ★ Sparring weekly | 35% | 55% | |
| Conversion | 1.5% | 3% | |
| MRR | $3,000 | $30,000 | |
| Store rating | 4.0 | 4.5 | |

Fill in the actuals honestly. If the ★ features didn't land, that's the answer to whether this game had a reason to exist — and it's better to know it from data than to keep building on an assumption.

← Back to [MASTER-CHECKLIST.md](MASTER-CHECKLIST.md)

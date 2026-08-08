# Phase 6 — Live-Ops & Events

> **Goal:** ship content without shipping a client build. This is what makes solo live-ops possible.
> **Est:** 6–8 weeks · **Docs:** `09`, `14`, `24`

**Gate to Phase 7:** a brand-new event can be created and launched from the admin panel with **zero client builds**

---

## 6.1 Event framework (`09`, `14 §7`)

- [ ] Event definitions as JSON, stored server-side, versioned
- [ ] Scoring rules composed from primitives (upgrade, train, kill, gather, spend, ★ arena win)
- [ ] Milestones and ranked leaderboards
- [ ] Reward grants, idempotent, audited
- [ ] Scheduling: start/end, timezone handling, recurrence
- [ ] Player-scope and alliance-scope events
- [ ] **Server-driven UI:** layout templates the client renders from data
- [ ] Remote Addressables group for event art (`18 §7`)
- [ ] Feature flag per event for instant disable

The server-driven UI is the load-bearing piece. If the client has to know what an event looks like, every event is a store release, and a solo developer cannot sustain that cadence.

## 6.2 The event catalogue (`09`)

- [ ] Arms Race — 6 rotating phases
- [ ] Alliance Duel — 6-day weekly
- [ ] Crazy Joe — wave defence
- [ ] Capitol Clash — President + Ministers + buffs
- [ ] Daily login ladder
- [ ] 8-day new-server progression ladder
- [ ] Bounty tasks
- [ ] Lucky wheel, truck rescue, marshal boss
- [ ] ★ Arena tournament event (weekly, ties into Phase 5)

## 6.3 Season pass (`13`)

- [ ] Season framework: duration, tiers, free + premium tracks
- [ ] XP sources across all layers, including ★ arena
- [ ] **Free track is genuinely rewarding** — this is a fairness pillar (`02`)
- [ ] Retroactive tier unlock on late purchase
- [ ] Season rollover without data loss

## 6.4 Admin panel (`20 §7`, `24 §2`)

- [ ] **Internal network only, no public exposure** — SSH tunnel access
- [ ] TOTP + IP allowlist
- [ ] Append-only audit log for every action
- [ ] Event scheduling and preview
- [ ] Player lookup, state inspection
- [ ] Grants and rollbacks (audited, reason required)
- [ ] Moderation queue: chat and ★ voice reports
- [ ] Feature flag toggles
- [ ] Config hot reload trigger

The admin panel is the highest-value target in the system. Every control above can grant currency or alter accounts, which is exactly why it never touches the public internet.

## 6.5 Push notifications

- [ ] Event start/end, milestone claimable
- [ ] Build/research complete
- [ ] Alliance help requested, ★ arena match starting
- [ ] Under a respectful daily cap; per-category opt-out
- [ ] Deep links open the exact target screen (`19 §2`)

---

## Gate checklist

- [ ] **Author a brand-new event in the admin panel and launch it to an installed client. No build. No app update.**
- [ ] Event art loads from the remote Addressables group
- [ ] Milestone claims are idempotent under retry
- [ ] Feature flag disables a live event instantly
- [ ] Season pass free track tested for genuine value
- [ ] Admin panel unreachable from the public internet — verified by trying
- [ ] Every admin action produces an audit row

→ Next: [phase-7-monetization.md](phase-7-monetization.md)

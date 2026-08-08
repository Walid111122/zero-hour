# 21 — Analytics & KPIs

> You cannot tune what you cannot see. Instrument from Phase 1, not Phase 9.
> Stack: **PostHog** (free tier or self-hosted) + **Sentry** (free tier).

---

## 1. The metrics that decide whether this works

| Metric | Target | What it tells you |
|---|---|---|
| **D1 retention** | > 40% | Is the first session good? |
| **D7 retention** | > 20% | Did the core loop take hold? |
| **D30 retention** | > 10% | Is there a reason to stay? |
| **Tutorial completion** | > 85% | Is onboarding broken? |
| **Sessions/day** | 4–6 | Are the appointment mechanics working? |
| **Session length** | 8–15 min | Engagement depth |
| **Alliance join rate (first hour)** | > 70% | Is the social hook landing? |
| **★ Weekly voice usage** | > 40% of alliance members | Is feature #1 working? |
| **★ Weekly sparring participation** | > 50% | Is feature #2 working? |
| **Conversion** | 2–3% | Monetization health |
| **ARPDAU** | $0.15–0.30 | Revenue per player |
| **Crash-free sessions** | > 99.5% | Technical quality |

**If D1 is below 30%, nothing else matters.** Fix the first session before building anything new. That's a hard rule, not a preference.

---

## 2. Event taxonomy

Naming: `category_object_action`, `snake_case`, past tense.

### Lifecycle
```
app_opened          { session_no, cold_start, load_ms }
session_started     { source, notification_id? }
session_ended       { duration_s, screens_visited }
tutorial_step       { step_id, completed, elapsed_s }      ← FTUE funnel
account_created     { method, hq_at_creation }
```

### Progression
```
hq_upgraded         { level, elapsed_since_prev_h, used_speedup }
building_upgraded   { def_id, level, help_count }
research_completed  { node_id, level }
troop_trained       { type, tier, count }
hero_acquired       { hero_id, source, rarity }
runner_stage        { stage, result, duration_s, retries }
```

### Economy
```
currency_earned     { currency, amount, source, balance_after }
currency_spent      { currency, amount, sink, balance_after }
resource_overflow   { resource, wasted }      ← storage-cap tuning signal
speedup_used        { job_type, minutes_saved, source }
```

### Social
```
alliance_joined     { method, minutes_since_install }
alliance_help_given / _received
alliance_gift_claimed { tier, source_kind }
chat_message_sent   { channel, lang, translated }
```

### ★ Voice
```
voice_channel_joined   { room_kind, participants }
voice_channel_left     { duration_s, ptt_presses }
voice_quality_degraded { reason, bitrate }
voice_user_muted / _blocked / _reported
voice_permission_denied
```

### ★ Arena
```
arena_room_created  { format, map, power_mode, is_ava }
arena_match_started { format, participants, voice_active }
arena_match_ended   { result, duration_s, elo_delta, dc_count }
arena_ladder_reward { tier, week }
ava_challenge_sent / _accepted / _declined
```

### Monetization
```
iap_offer_shown     { offer_id, trigger, context }
iap_purchase_started / _completed / _failed { offer_id, price_usd, reason? }
ad_rewarded_completed { placement }
```

**Discipline:** every event has a documented schema in `docs/analytics-schema.md`. An event with inconsistent properties is worse than no event, because it produces confidently wrong dashboards.

---

## 3. Funnels to watch

| Funnel | Steps |
|---|---|
| **FTUE** | Install → first shot → stage 1 clear → base seen → first build → HQ 2 → HQ 5 → alliance joined |
| **Alliance** | Alliance screen → browse → apply → accepted → first help given |
| **★ Voice** | Voice tab → permission prompt → granted → joined → first PTT press |
| **★ Arena** | Arena tab → room list → joined → match started → match finished → second match |
| **Purchase** | Offer shown → tapped → store opened → completed |

The second-match step in the arena funnel is the one that matters most. Anyone who plays two sparring matches has understood the feature; anyone who stops at one did not enjoy it.

---

## 4. Dashboards

| Dashboard | Contents |
|---|---|
| **Daily health** | DAU, new users, D1/D7/D30, crash rate, revenue, ARPDAU |
| **FTUE** | Tutorial funnel with per-step drop-off, time per step |
| **Progression** | HQ level distribution, median time per level, stall points |
| **Economy** | Faucet/sink by source, overflow waste, currency balances by cohort |
| **Social** | Alliance membership %, help volume, chat volume, translation usage |
| **★ Voice** | Adoption, session length, PTT rate, mute/report rate, quality issues |
| **★ Arena** | Participation, matches/player, format popularity, ELO distribution, AvA activity |
| **Monetization** | Conversion, ARPPU, offer performance, VIP distribution |
| **Technical** | p50/p95 API latency, error rate, ★ arena tick overruns, voice bandwidth |

---

## 5. Privacy-respecting implementation

| Requirement | Approach |
|---|---|
| Consent | GDPR/CCPA consent prompt before any analytics send; deny is fully respected |
| Anonymity | Internal player UUID only. **No device advertising ID, no email, no name** in analytics |
| Minimisation | Collect what answers a specific question; nothing "just in case" |
| Retention | 90 days raw events, aggregates kept longer |
| Deletion | Account deletion purges analytics rows for that UUID |
| Transport | Batched every 30 s or 20 events, gzipped, retried offline |
| ★ Voice | **Never any audio content in analytics** — only counts, durations, and quality metrics |

**No PII in analytics, ever.** It is not needed to answer any question we have, and it converts a low-risk system into a liability.

---

## 6. Alerting (Sentry + PostHog)

| Alert | Threshold |
|---|---|
| Crash rate | > 1% of sessions |
| API error rate | > 2% over 5 min |
| API p95 latency | > 800 ms |
| DAU drop | > 20% day-over-day |
| D1 drop | > 5 points week-over-week |
| Revenue drop | > 30% day-over-day |
| ★ Arena tick overrun | > 1% of ticks |
| ★ Voice failure rate | > 5% of joins |
| Purchase validation failures | > 1% |
| Suspicious-activity flags | > 50/hour |

---

## 7. A/B testing (Phase 10)

Server-side flags with sticky bucketing by player UUID.

Candidates, in priority order:
1. FTUE variants (the highest-leverage test available)
2. Alliance prompt timing
3. ★ Arena unlock level (HQ 8 vs HQ 6)
4. Starter pack price point
5. Storage cap hours (8 vs 12)

**Rules:** one experiment per surface at a time, minimum 1,000 users per arm, minimum 7 days, pre-registered success metric. Peeking at results daily and stopping when the numbers look good is how teams convince themselves of things that aren't true.

---

## Next
- `12-economy-balance-model.md` — what these metrics feed
- `25-launch-ua-plan.md`

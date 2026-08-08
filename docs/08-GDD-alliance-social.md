# 08 — GDD: Alliances & Social

> **Pillar 2: your alliance is the game.** This layer is where both new features live, and where long-term retention is won or lost.
> Phase 4.

---

## 1. Structure

| Property | v1 value |
|---|---|
| Max members | **60** (raise to 100 when DAU supports it) |
| Ranks | R1 → R5 |
| Tag | 3 uppercase characters, unique per state |
| Name | 4–20 chars, moderated |
| Join | Open, application-only, or invite-only |
| Min HQ to create | 5 |
| Leave cooldown | 24 h before joining another alliance |

**Why 60 and not 100:** at low DAU, a 100-slot alliance is a half-empty room. 60 full slots feels alive; 100 slots at 55% feels dying. Raise the cap when population justifies it.

### Ranks & permissions
| Rank | Title | Can |
|---|---|---|
| R5 | Leader | Everything; ★ issue AvA challenges; disband; transfer |
| R4 | Officer | Accept/kick members, start rallies, ★ moderate voice, ★ create arena rooms, edit notices |
| R3 | Veteran | Start rallies, ★ create sparring rooms |
| R2 | Member | Standard |
| R1 | Recruit | Standard minus territory actions |

---

## 2. Alliance Help — the keystone

```
Player starts a build/research/training
   → optional "Request Help" (1 tap, or auto-request setting)
   → appears in the alliance help list
   → each member who clicks removes 1% of REMAINING time
   → max 30 clicks (−30%)
   → helper earns 1 Valor Badge per help (capped ~60/day)
```

**Why this is the most important mechanic in the social layer:**
- It is **free** — no resource cost to either party
- It requires **other people** — you cannot self-help
- It is worth **~30% of your progression speed**
- Helping is a **zero-cost altruistic act that pays the helper**

The result: an alliance-less player is meaningfully behind, so everyone joins one. And once they are in an alliance, they are in range of voice chat, arenas, gifts, duels, and war — all the systems that actually retain them.

**Implementation notes:** "Help All" button (one tap helps every pending request). Push notification when help is requested, throttled to avoid spam.

---

## 3. Alliance Tech

- Members donate resources → **alliance tech points**
- Points unlock alliance-wide permanent buffs across 3 branches (Economy / Battle / Territory)
- Donating earns **Valor Badges** and **individual contribution rank**
- Contribution leaderboard visible to all → social pressure to contribute

Alliance tech is the mechanism that makes a mature alliance materially better than a new one, which gives alliances something to build together over months.

---

## 4. Alliance Gifts — the monetization multiplier

```
Any member makes an IAP purchase
   → a gift chest drops for EVERY member
   → chest tier scales with purchase size
   → an announcement names the buyer
   → members open chests, earn Valor + resources + occasional diamonds
```

This is the single most effective monetization-through-social-design mechanic in the genre. It:
1. Converts spending into **social status** — the buyer is publicly thanked
2. Makes 59 other players **actively grateful** for whale spending
3. Creates a virtuous cycle where alliances *recruit* spenders
4. Gives F2P players a real, tangible benefit from being in an active alliance

**Ship this at launch.** It costs almost nothing to build and it is worth a large multiple of its development time.

Also drops gifts on: alliance milestones, ★ AvA victories, rally boss kills, event wins.

---

## 5. Alliance Shop

Spend **Valor Badges** on: speedups, teleports, shields, hero shards, troop boosts, cosmetic frames, ★ arena entry tickets.

Stock rotates weekly. Valor sources: helps, donations, alliance events, ★ arena matches.

---

## 6. Chat

### 6.1 Channels
| Channel | Scope | Notes |
|---|---|---|
| **World** | All players in the state | Rate-limited, moderated |
| **Alliance** | Alliance members | The primary channel |
| **Private** | 1:1 | Friend list |
| **System** | Server → player | Announcements, battle reports |
| **★ Voice channels** | See `10` | |

### 6.2 Auto-translate (self-hosted, $0)
- **LibreTranslate** running on the same VM
- Each message stores its source language; a per-user setting sets target language
- Translation is **on-demand per message** (tap to translate) plus an "always translate" toggle
- Results cached in Redis keyed by `hash(text)+targetLang` — most chat is repetitive, so the cache hit rate is high and CPU cost stays low

Auto-translate is essential for our target markets (`02 §4`). An Arabic-speaking player and a Spanish-speaking player must be able to coordinate a war.

### 6.3 Moderation
- Profanity filter per language, server-side
- Rate limits: 1 msg/2 s, 20 msgs/min
- Report → moderation queue with surrounding context
- Mute levels: 10 min → 1 h → 24 h → permanent
- Link/invite blocking in World chat (stops competitor poaching and scam links)

---

## 7. Mail

Categories: System · Battle Reports · Alliance · Rewards
- Attachments claimable, bulk "claim all"
- Auto-delete read mail after 7 days, unread after 30

---

## 8. Friends

- Add via profile, chat, or recent-battle list
- Shows online status and current alliance
- Enables private chat and ★ direct sparring invites (cross-alliance sparring is Phase 6+)
- Cap: 100

---

## 9. Alliance profile & identity

Identity is what makes an alliance worth belonging to. Give players things to be proud of:

- Banner: emblem shape + colour + pattern (unlocked by achievements)
- Description + recruitment notice
- **War record** ★ — AvA wins/losses, current streak, seasonal rank
- **Hall of Fame** ★ — past Alliance Champions (sparring ladder winners)
- Alliance level, total power, member list with power/rank/last-active
- Territory map preview

---

## 10. Alliance events (Phase 6, detailed in `09`)

| Event | Cadence |
|---|---|
| Alliance Duel | Weekly, 6 days |
| Crazy Joe | Weekly |
| ★ AvA Arena | Weekly + on-demand |
| Rally bosses | Continuous |
| Capitol Clash | Cyclic |

---

## 11. Anti-toxicity design

Social features cut both ways. Explicit protections:

| Risk | Mitigation |
|---|---|
| Leader abandons alliance | Auto-transfer R5 to highest-ranked active member after 7 days inactive |
| Kick abuse | Kick requires R4+; kick log visible to all members; mass-kick rate limited |
| Recruitment spam | World chat rate limits + link blocking |
| Harassment via private chat | Block list, report, mute |
| ★ Voice harassment | Full moderation spec in `10 §6` |
| Freeloading | Contribution leaderboard makes participation visible without forcing it |
| Alliance-hopping for gifts | 24 h leave cooldown; gifts require 24 h membership |

---

## 12. Data & schema notes

| Table | Key fields |
|---|---|
| `alliances` | id, stateId, tag, name, level, techPoints, treasury, warRecord |
| `alliance_members` | allianceId, playerId, rank, joinedAt, contribution, lastActive |
| `alliance_help_requests` | id, allianceId, playerId, jobType, jobId, helpsReceived, expiresAt |
| `alliance_tech` | allianceId, nodeId, level |
| `alliance_gifts` | id, allianceId, tier, sourcePlayerId, createdAt |
| `alliance_gift_claims` | giftId, playerId, claimedAt |
| `chat_messages` | id, channel, scopeId, playerId, body, lang, createdAt |

Full DDL in `15-data-schema.md`.

---

## 13. Phase 4 acceptance criteria
- [ ] Create / join / apply / invite / leave with cooldown
- [ ] R1–R5 permissions enforced **server-side**
- [ ] Alliance Help with 30-click cap + Help All + Valor rewards
- [ ] Alliance Tech donation and buffs applied in combat
- [ ] **Alliance Gifts firing on IAP** with public announcement
- [ ] Alliance Shop with weekly rotation
- [ ] Chat: world / alliance / private with rate limits
- [ ] LibreTranslate integrated with Redis cache
- [ ] Mail with bulk claim
- [ ] Friends list
- [ ] Alliance profile with banner + war record
- [ ] All anti-toxicity protections in §11 implemented
- [ ] 70%+ of test players join an alliance within their first hour

---

## Next
- `09-GDD-events-liveops-calendar.md`
- `10-FEATURE-voice-chat.md` ★ — the alliance made audible
- `11-FEATURE-arena-rooms.md` ★ — the alliance made competitive

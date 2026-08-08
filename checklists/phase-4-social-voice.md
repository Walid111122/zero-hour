# Phase 4 — World Map, Alliances & ★ Voice Chat

> **Goal:** the social layer, and the first of the two features that justify this game's existence.
> **Est:** 8–10 weeks · **Docs:** `07`, `08`, `10`, `16`, `22`

**Gate to Phase 5:** 100 simulated players on one world map with no desync · voice holds 10 concurrent talkers for 30 minutes without a leak · alliance help and gift loops complete end to end

---

## 4.1 World map (`07`)

- [ ] Chunked grid world, spatial index, streamed by viewport
- [ ] Tile types: empty, resource node, zombie, player base, alliance structure
- [ ] Pan/pinch navigation, jump-to-coordinate, minimap
- [ ] Marches: send, travel time by distance, recall, queue limits
- [ ] March capacity from buildings + tech
- [ ] Resource gathering with server-resolved completion
- [ ] Zombie units and bosses
- [ ] PvP attack + scout
- [ ] Shields: newbie, purchased, post-defeat grace
- [ ] Teleports: random, targeted, alliance
- [ ] Rally: initiate, join window, combined force resolution
- [ ] Battle reports delivered by mail

## 4.2 Alliances (`08`)

- [ ] Create, search, apply, invite, join, leave, kick
- [ ] Ranks R1–R5 with a permission matrix, enforced server-side
- [ ] Alliance help: request, help, acceleration formula, dedupe via `alliance_help_log`
- [ ] Gifts: tiers, claim window, purchase-triggered gifts
- [ ] Donations → alliance tech tree
- [ ] Alliance shop with earned currency
- [ ] Territory: flags, turrets, buff radius
- [ ] Alliance profile, banner, announcement, recruitment settings

## 4.3 Chat & mail (`08`)

- [ ] Channels: world, alliance, private, system
- [ ] SignalR realtime delivery with reconnect (`16`)
- [ ] LibreTranslate integration, per-user target language, original text toggleable
- [ ] Profanity filter, rate limits, mute, block, report
- [ ] Mail: system, battle reports, rewards with attachments
- [ ] 30-day chat retention (`22 §2`)

## 4.4 ★ Voice chat (`10`) — the feature

### Infrastructure
- [ ] LiveKit self-hosted on the VM, coturn for restrictive NATs (`24 §3`)
- [ ] UDP ports open and verified from a mobile network
- [ ] Server mints scoped join tokens; membership checked on every mint
- [ ] `com.unity.webrtc` integrated

### Client
- [ ] `IVoiceService` abstraction with a mock implementation for tests (`10 §4.1`)
- [ ] Persistent voice widget, draggable, **survives scene changes** (`17 §4`)
- [ ] Channels: alliance main, rally, officer, ad-hoc
- [ ] Push-to-talk **and** open-mic modes
- [ ] 8-speaker cap with a speaking queue
- [ ] Speaking indicator ring on avatars
- [ ] Audio ducking: music −12 dB, SFX −6 dB while anyone speaks (`18 §5`)
- [ ] Per-user volume, local mute, block
- [ ] Bluetooth and wired headset handling
- [ ] Graceful degradation on poor networks; automatic bitrate reduction

### Safety & compliance (`10 §6`, `22`)
- [ ] Age gate at registration, neutral
- [ ] **Under-13: voice fully disabled, no override**
- [ ] Explicit consent + mic permission requested in context at first use
- [ ] 30-second local ring buffer; audio leaves the device **only on report**
- [ ] Reported clips encrypted at rest, **auto-deleted after 7 days** — verified with a test
- [ ] Report flow: 3 taps maximum
- [ ] Mute enforced **at the SFU**, not the client (`20 §1`)
- [ ] Moderation actions logged append-only with actor, reason, evidence
- [ ] Full text-chat parity so voice is never required to participate (`19 §6`)

### Analytics (`21`)
- [ ] `voice_channel_joined` / `_left`, PTT counts, quality degradation
- [ ] `voice_user_muted` / `_blocked` / `_reported`
- [ ] **No audio content in analytics, ever**

---

## Gate checklist

- [ ] 100 simulated players on one map, no desync, server CPU within budget
- [ ] 10 concurrent talkers for 30 minutes: no memory growth, no dropped audio
- [ ] Voice survives a scene change (Base → World) without disconnecting
- [ ] Voice survives cellular ↔ WiFi handover
- [ ] Battery cost under 6%/hour with voice active (`23 §8`)
- [ ] Under-13 account cannot access voice by any path
- [ ] A reported clip is deleted at 7 days — verified, not assumed
- [ ] **In a 20-person test alliance, do people actually use it?** Adoption above 15%
- [ ] Does a rally with voice feel different from one without?

That last pair of questions is the real gate. The infrastructure working is necessary but not sufficient — if people join the channel and stay silent, the UX needs work before Phase 5 builds on top of it.

→ Next: [phase-5-arena.md](phase-5-arena.md)

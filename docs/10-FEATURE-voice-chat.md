# 10 — ★ FEATURE: Alliance Voice Chat

> **Your feature #1.** Phase 4.
> Cost target: **$0** — self-hosted LiveKit on the existing free VM.

---

## 1. Why this feature exists

**The gap:** games in this genre ask alliances to coordinate timed, high-stakes events — rallies, wars, Capitol Clash — using **typed text, across language barriers, in real time.** It doesn't work. So serious alliances migrate to Discord.

**The cost of that migration:** the game **leaks its own social graph** to a third party. Once the friendships live on Discord, the game becomes a chore the group does together, and the group can move to a different game overnight without losing anything.

**The thesis:** a player whose friendships live *inside your app* does not churn. Voice chat is a retention feature disguised as a convenience feature. It is the highest-leverage social investment available to a game in this genre, and almost nobody in the category has it done well.

### Secondary benefits
- Makes rallies and ★ AvA arena matches dramatically more fun and more attended
- Solves the language problem better than translation does — tone, urgency, and names cross languages even when words don't
- Alliance leaders (the 5% "socialite" segment that retains everyone else) get their most-wanted tool
- Strong differentiator in store listings and creatives

---

## 2. Channel model

| Channel | Members | Lifetime | Default mode | Created by |
|---|---|---|---|---|
| **Alliance Main** | All (≤60) | Permanent | Push-to-talk | System, on alliance creation |
| **Officers** | R4–R5 | Permanent | PTT | System |
| **Rally** | Rally participants (2–20) | Auto: rally start → resolve | PTT | System, on rally start |
| **★ Arena Room** | Room participants | Room lifetime | PTT | System, on room create (`11`) |
| **★ AvA War** | Match roster (≤20/side) | Match window ±15 min | PTT | System, on match schedule |
| **Squad** | Ad-hoc, ≤8 | Manual, or 30 min idle | Open mic | Any member |

### Rules
- A user is in **at most one active voice channel** at a time. Joining another leaves the first. (Multi-channel is a UX disaster on mobile and doubles bandwidth.)
- Alliance Main is always available and never auto-joins — the user chooses.
- Rally/Arena/War channels **prompt** to join with a one-tap accept, and auto-leave when the context ends.
- **Speaker cap: 8 simultaneous talkers** per channel. Beyond that, the loudest 8 are transmitted and others queue. This is a hard bandwidth and comprehensibility limit — 20 people talking at once is noise, not communication.

---

## 3. User experience

### 3.1 Entry points
- **Alliance Center** building → Voice tab
- Persistent **floating voice widget** when connected (draggable, collapsible, shows active speakers)
- Alliance chat screen → voice toggle in the header
- Rally screen / ★ arena lobby → "Join Voice" prompt

### 3.2 The floating widget
The most important UI element of the feature. When connected:
- Small pill showing channel name + participant count
- Avatars of the currently-speaking users with an animated ring
- **Big PTT button** (thumb-reachable, bottom-right by default, draggable)
- Tap to expand: full participant list, per-user volume/mute, leave

It must remain usable while the player is doing anything else — building, marching, fighting. **Voice is a background activity layered over gameplay**, never a separate screen you have to sit in.

### 3.3 Push-to-talk vs open mic
- **PTT is the default**, everywhere. Battery, background noise, and privacy all demand it.
- Open mic is opt-in per channel, with **VAD** (voice activity detection) and a visible "your mic is live" indicator that cannot be dismissed.
- PTT has a 150 ms release tail so word-endings aren't clipped.
- Hardware volume-down long-press as an optional PTT key (Android).

### 3.4 Feedback & clarity
- Speaking users show an animated ring in the widget, the participant list, and the alliance member list
- Your own mic shows a live input-level meter while transmitting
- Connection quality indicator (green/amber/red) per user
- Join/leave uses a **soft two-note chime**, never a voice announcement
- If you press PTT while muted by a moderator, a clear message says so — never fail silently

---

## 4. Technical design

### 4.1 Stack ($0)

| Layer | Choice | Licence / cost |
|---|---|---|
| SFU server | **LiveKit** | Apache-2.0, self-hosted, free |
| Client transport | `com.unity.webrtc` + LiveKit Unity SDK | Free |
| Codec | **Opus**, mono | Free, royalty-free |
| Signalling | LiveKit WebSocket + JWT room tokens | Free |
| TURN relay | **coturn** on the same VM | Free |
| Host | Oracle Cloud Always Free (4 ARM cores / 24 GB) | Free |

**Why LiveKit over the alternatives:**
- Vivox: free tier exists but is a vendor dependency with terms that can change, and no self-host option
- Agora: pay-per-minute, fails the $0 requirement at scale
- Photon Voice: CCU-capped free tier
- Raw mesh WebRTC: **does not scale** — 60 peers × 59 connections each is impossible on mobile. An SFU is mandatory above ~6 participants.

**Phase 1 shortcut:** prototype against **Vivox free tier** (zero infra work) behind the `IVoiceService` interface, then swap to LiveKit for production. The abstraction makes it a one-file change.

### 4.2 Architecture

```
Unity Client
  └─ IVoiceService  ← abstraction (swap providers freely)
       ├─ LiveKitVoiceService   (production)
       └─ MockVoiceService      (editor/tests, no network)
              │
              │ 1. request token
              ▼
      ZeroHour.Server /voice/token
        - verifies alliance membership & rank
        - verifies not banned from voice
        - mints short-lived JWT (TTL 60 s, single room)
              │
              │ 2. connect with JWT
              ▼
         LiveKit SFU  (same VM, :7880)
           - Opus, mono, 16–24 kbps
           - server-side mute enforcement
           - room membership mirrors alliance state
              │
              ▼
         coturn (TURN relay for restrictive NATs)
```

**Critical:** the game server, not LiveKit, is the authority on who may join which room. LiveKit only validates the JWT the game server minted. When a player is kicked from the alliance, the server calls LiveKit's admin API to **eject them immediately** — membership is never allowed to drift.

### 4.3 Audio settings

| Setting | Value | Why |
|---|---|---|
| Codec | Opus mono | Best quality per bit for speech |
| Bitrate | 24 kbps WiFi / **12 kbps cellular** | Adaptive, cellular-friendly |
| Sample rate | 48 kHz capture → 16 kHz transmit | Speech doesn't need more |
| Frame size | 20 ms | Standard latency/overhead balance |
| **AEC** | On | **Mandatory** — speakerphone without echo cancellation is unusable |
| **Noise suppression** | On | Mobile gaming happens in noisy places |
| **AGC** | On | Users hold phones at wildly different distances |
| DTX | On | Don't transmit silence — big bandwidth saving |
| Jitter buffer | Adaptive 40–200 ms | Mobile networks are erratic |

### 4.4 Bandwidth & cost model

```
Per talker uplink   : ~24 kbps
Per listener downlink: 8 talkers × 24 kbps ≈ 192 kbps worst case
Typical (2 talkers) : ~48 kbps down
```

| Scenario | Server bandwidth |
|---|---|
| 1 channel, 60 listeners, 4 talkers | 4×24 up + 60×96 down ≈ **5.9 Mbps** |
| 10 concurrent channels, average load | ≈ **20–30 Mbps** |
| Monthly, 10 channels × 4 h/day | **≈ 1.3 TB** |

Oracle free tier allows **10 TB/month egress**. Voice fits comfortably with room to spare. CPU: LiveKit SFU only forwards packets (no transcoding), so a 4-core ARM box handles hundreds of concurrent participants.

**Verdict: alliance voice chat at $0 is genuinely feasible at our target scale.** It stops being free somewhere around 5–10k concurrent voice users, which is a problem we would be delighted to have.

### 4.5 Mobile-specific handling

| Concern | Handling |
|---|---|
| Battery | PTT default, DTX on, auto-disconnect after 15 min idle, no video ever |
| Background | Voice continues in background (foreground service on Android) with a persistent notification |
| Interruption | Phone call → auto-mute + auto-resume after |
| Bluetooth/headset | Route correctly; handle mid-session device changes |
| Low-end devices | Auto-disable voice during heavy scenes (arena battle) if FPS drops below 25 for 3 s, with a user-visible notice |
| Thermal | Reduce bitrate to 12 kbps under thermal pressure |
| Data saver | Respect OS data-saver; warn before joining on cellular; per-user "WiFi only" setting |

---

## 5. Permissions & controls

### 5.1 Rank-based (server-enforced)
| Action | Min rank |
|---|---|
| Join Alliance Main | R1 |
| Join Officers | R4 |
| Create Squad channel | R1 |
| Create Rally/Arena channel | R3 |
| **Server-mute a member** | R4 |
| **Kick from voice** | R4 |
| Ban a member from voice (24 h) | R5 |
| Lock a channel | R4 |

### 5.2 Per-user (client-side, always available)
- Local mute of any individual (persists across sessions)
- Per-user volume slider
- **Block** (also blocks text chat) — a blocked user is never heard, ever
- Global "disable voice entirely" setting
- "Never auto-prompt for voice" setting

**Principle:** a user must always be able to make someone stop being audible to them, instantly, without needing a moderator. Local mute is one tap from the participant list and one tap from the speaking indicator.

---

## 6. Safety, moderation & legal

**This section is a legal requirement, not a feature wishlist.** Voice chat carries real liability. Get it wrong and you face store removal, regulatory action, or worse.

### 6.1 Age gating
- Voice chat requires a declared age of **13+**
- Under-13 accounts: voice **disabled entirely**, no override, no parental unlock in v1 (COPPA verifiable-parental-consent is expensive and complex — avoid it by not offering the feature)
- Age is collected at registration and stored server-side
- The Play Store data safety declaration must state that voice data is processed

### 6.2 Reporting
```
User taps Report on a participant
   → client uploads the last 30 s from a local ring buffer
   → server stores encrypted, retention 7 days, then auto-deleted
   → entry created in the admin moderation queue
   → reported user notified that a report was filed (deterrent, no details)
   → rate limit: 5 reports per user per day (prevents report-brigading)
```

**The 30-second local ring buffer is the key design decision.** It means we do **not** record or store voice continuously — audio only ever leaves the device when a user explicitly reports. That is dramatically better for privacy, storage cost, and legal exposure than server-side recording, and it is still sufficient evidence for moderation.

### 6.3 Moderation actions
| Level | Action |
|---|---|
| 1 | Warning |
| 2 | 1 h voice mute |
| 3 | 24 h voice mute |
| 4 | 7 day voice ban |
| 5 | Permanent voice ban |
| 6 | Account action (text + voice + play) |

All actions logged with actor, target, reason, evidence reference, and timestamp in an append-only audit log.

### 6.4 Privacy & compliance
- **Explicit consent** before first mic use, with a clear explanation of what is and isn't recorded
- Microphone permission requested **only** at first voice use, never at app start
- Privacy policy explicitly covers voice: not stored except on report, 7-day retention, purpose limited to moderation
- **GDPR:** right to erasure covers reported clips; data processing agreement not needed since we self-host
- **COPPA:** handled by the under-13 block above
- **DSA (EU):** reporting mechanism + transparency about moderation actions
- Data residency: EU users' reported clips stored in an EU region if/when we expand hosting

### 6.5 Rate limits & abuse prevention
- Max 20 channel joins per hour per user (stops join/leave spam)
- Server-side mute enforced **at the SFU**, not just the client — a modified client cannot bypass it
- Alliance-level voice disable (R5 can turn off voice for the whole alliance)
- Automatic disconnect after 4 h continuous (prevents forgotten open mics)

---

## 7. Analytics

| Event | Purpose |
|---|---|
| `voice_channel_joined` | Adoption |
| `voice_channel_left` (with duration) | Engagement depth |
| `voice_ptt_pressed` (aggregated count) | Actual usage vs passive listening |
| `voice_quality_degraded` | Network/infra health |
| `voice_muted_user` / `voice_blocked_user` | Toxicity signal |
| `voice_report_filed` | Moderation load |
| `voice_permission_denied` | Onboarding friction |

**North-star metric: % of alliance members using voice weekly.**
Target ≥ 40%. Below 15% means the feature is failing and needs UX work, not more features.

**The retention hypothesis to validate:** players who use voice ≥ 1×/week should show materially higher D30 than those who never do. Measure it explicitly — this is the entire justification for the feature's cost.

---

## 8. Implementation plan (Phase 4)

| Step | Deliverable |
|---|---|
| 1 | `IVoiceService` interface + `MockVoiceService` for editor/tests |
| 2 | Server `/voice/token` endpoint with membership + rank + ban checks |
| 3 | LiveKit + coturn deployed via Docker Compose on the VM |
| 4 | `LiveKitVoiceService` — connect, publish, subscribe, disconnect |
| 5 | Floating voice widget + PTT button |
| 6 | Participant list, speaking indicators, local mute/volume |
| 7 | Alliance Main + Officers channels |
| 8 | Rally / ★ Arena / ★ War auto-channels with prompts |
| 9 | Rank-based moderation (server-mute, kick, ban) enforced at the SFU |
| 10 | Age gate, consent flow, permission request timing |
| 11 | Report flow with 30 s ring buffer + admin queue |
| 12 | Mobile handling: background, interruption, battery, thermal, data saver |
| 13 | Analytics events |
| 14 | Load test: 60 participants, 8 talkers, 30 min, no leak |

---

## 9. Acceptance criteria
- [ ] 10 concurrent talkers, 30 minutes, no memory leak, no crash
- [ ] AEC verified: two devices on speakerphone in one room produce no echo
- [ ] PTT latency (press → transmitting) < 100 ms
- [ ] Mouth-to-ear latency < 300 ms on 4G
- [ ] Auto-degrades to 12 kbps on cellular
- [ ] Continues correctly in background; survives an incoming phone call
- [ ] Kicked-from-alliance member is ejected from voice within 2 s
- [ ] **Server-mute cannot be bypassed by a modified client** (verified at SFU)
- [ ] Under-13 account cannot access voice under any path
- [ ] Report flow uploads exactly 30 s and auto-deletes at 7 days
- [ ] Local mute/block persist across app restarts
- [ ] Battery drain < 6%/hour with voice connected in PTT mode
- [ ] Bandwidth per user < 200 kbps down at worst case
- [ ] Works on the lowest-spec target device (2020 midrange Android)

---

## 10. Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Moderation liability | 🔴 | Age gate, reporting, retention policy, clear ToS (`22`) |
| Toxicity drives players away | 🟠 | One-tap local mute/block; R4 moderation; block is absolute |
| Battery complaints | 🟠 | PTT default, DTX, idle disconnect, thermal throttling |
| Infra cost at scale | 🟡 | Free tier covers ~5–10k concurrent; revenue arrives first |
| WebRTC on Unity is finicky | 🟠 | `MockVoiceService` keeps development unblocked; Vivox as fallback |
| Low adoption | 🟡 | Auto-prompts at high-value moments (rally, arena), not cold |
| Server-mute bypass via modified client | 🔴 | Enforce at the SFU, never client-side only |

---

## Next
- `11-FEATURE-arena-rooms.md` ★ — the feature this one makes fun
- `22-legal-compliance.md` — the legal detail behind §6
- `checklists/phase-4-social-voice.md`

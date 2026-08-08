# 20 — Security & Anti-Cheat

> **The client is hostile.** Assume every byte it sends is attacker-controlled.
> Security is architectural (`14`), not a Phase 9 feature.

---

## 1. Threat model

| Threat | Impact | Defence |
|---|---|---|
| Memory editing (GameGuardian) | Fake resources locally | Server is authoritative — local edits change nothing real |
| Modified APK | Bypassed client checks | All checks exist server-side too |
| Packet replay | Duplicate rewards | Nonce + sequence per request; idempotency keys |
| Packet forging | Arbitrary actions | Intent-only API, full server validation |
| Speed hacking | Faster timers | Timers are server timestamps; client time is never trusted |
| Runner score forgery | Fake rewards | **Server re-simulates the input log** |
| ★ Arena cheating | Rigged matches | Server-authoritative sim; intent validation; replay audit |
| ★ Voice mute bypass | Harassment | Mute enforced at the SFU, not the client |
| Purchase forgery | Free currency | Server-side receipt validation, always |
| Account takeover | Theft | Refresh-token rotation, device binding, no password recovery via chat |
| Bot farming | Economy inflation | Behavioural detection, rate limits, device fingerprinting |
| Alt-account abuse | Reward multiplication | Device + IP correlation, join cooldowns |
| SQL injection | Catastrophic | Parameterised queries only, no string concatenation, ever |
| DDoS | Downtime | Cloudflare free tier + Nginx rate limits |

---

## 2. Core principle: intents, not results

```
❌ POST /api/runner/complete { score: 999999, rewards: [...] }
✅ POST /api/runner/complete { stageId: 12, inputLog: <bytes>, seed: 8842 }
      → server runs RunnerSim.Simulate(stage, inputLog, seed)
      → server computes the score and the rewards
      → client's claimed score is never read
```

**Every endpoint follows this shape.** The client says what the player *did*; the server decides what *happened*. That single rule eliminates the majority of cheat vectors before they exist.

### Applied per system
| System | What the client sends | What the server does |
|---|---|---|
| Building upgrade | `buildingId` | Checks cost/prereqs, deducts, schedules |
| Runner stage | Input log + seed | Re-simulates, awards |
| March | Target tile + squad | Validates range/troops, computes travel time |
| Battle | Nothing | Resolves entirely server-side |
| ★ Arena | Input intents | Runs the authoritative sim at 20 Hz |
| Purchase | Store receipt | Validates with Google, then grants |
| Alliance help | `requestId` | Checks membership, dedupes via `alliance_help_log` |

---

## 3. Authentication

```
Device ID → guest account (immediate play, no friction)
   ↓ optional upgrade
Google Sign-In / email
   ↓
Access token  (JWT, 1 h TTL, contains playerId + stateId)
Refresh token (30 d, rotating, single-use, stored in the OS keystore)
```

- Tokens signed with a key held only in server environment variables
- Refresh-token rotation with **reuse detection** — a replayed refresh token invalidates the whole family and forces re-login
- Device binding on refresh; a new device requires re-authentication
- Rate limit: 5 auth attempts per IP per minute
- **No account recovery through support chat** — social engineering is the most common takeover vector in this genre

---

## 4. Runner validation (the interesting case)

The runner is the one place a player has continuous real-time input that produces rewards. It's the obvious cheat target.

```
Client plays stage → records InputLog (frame, action) + seed
Client submits log
Server: RunnerSim.Simulate(stageDef, inputLog, seed, playerStats)
   → deterministic result, fixed-point
   → compares its own score with the client's claim
   → mismatch ⇒ reject, log, increment suspicion
   → also validates: log length plausible, no superhuman input rate,
     no impossible input timing (< 30 ms between distinct taps)
```

Cost: a stage sim is sub-millisecond, so full validation on every submission is affordable. **We validate 100% of runner submissions**, not a sample.

---

## 5. ★ Arena anti-cheat

Because the server runs the authoritative sim, the arena is structurally hard to cheat. The remaining vectors and their answers:

| Vector | Defence |
|---|---|
| Forged intents | Validated per `16 §6` |
| Intent flooding | Rate limit 20/s, excess dropped and counted |
| Controlling another squad | Ownership check on every intent |
| Cooldown bypass | Cooldowns live in server state only |
| Match-fixing / collusion | Diminishing ELO on repeated pairings; anomaly detection on win patterns |
| Alt-account ELO farming | Device fingerprint + IP correlation; ELO requires distinct opponents |
| Disconnect abuse (rage-quit to avoid a loss) | Disconnect after 30 s counts as a loss |
| Replay tampering | Replays are server-generated; the client never uploads one |

**Every match's replay is a re-simulatable audit trail.** Any disputed result can be recomputed from the input log.

---

## 6. Bot & farm detection

Signals, scored together rather than individually:

| Signal | Weight |
|---|---|
| Perfectly regular action intervals (low variance) | High |
| 24/7 activity with no gaps | High |
| Identical action sequences across accounts | High |
| Many accounts from one device fingerprint | High |
| Resource transfer patterns pointing to one recipient | Medium |
| No IAP, no chat, no social interaction, high activity | Medium |
| Impossible reaction times in the runner | High |

**Response ladder:** flag → shadow-limit rewards → captcha challenge → suspension → ban.

The deliberate order matters: automated permanent bans on a heuristic will punish innocent players. The ladder gives a real player a chance to fail gracefully, and a bot farm no path forward.

---

## 7. Server hardening

| Area | Measure |
|---|---|
| Transport | TLS 1.3 only, HSTS, certificate pinning in the client |
| Rate limiting | Per-IP and per-player, per-endpoint budgets |
| Input validation | Every field range-checked; reject rather than clamp on suspicious values |
| SQL | Parameterised queries only; EF Core / Dapper with parameters |
| Secrets | Environment variables, never in git; rotated on any suspicion |
| Dependencies | Pinned exact versions; `dotnet list package --vulnerable` in CI |
| Logging | No PII, no tokens, no receipts in logs |
| **Admin panel** | TOTP, IP allowlist, **no public internet exposure** (SSH tunnel only), append-only audit log |
| DB access | App user has no DDL rights; migrations run as a separate role |
| Backups | Encrypted at rest, off-VM |
| Container | Non-root user, read-only filesystem where possible, no host network |

**The admin panel is the highest-value target in the system** — it can grant currency and modify accounts. It must never be reachable from the public internet. That single decision removes most of its risk.

---

## 8. Client-side measures (defence in depth, not primary)

These slow attackers down; they never *prevent* anything. The server is the real defence.

- IL2CPP (native compilation, harder to decompile than Mono)
- Sensitive in-memory values obfuscated (`ObscuredInt` pattern)
- Root/emulator detection → logged as a signal, **not** a block (many legitimate players use rooted devices, and blocking them costs more than it saves)
- Integrity check on config tables (hash comparison against the server)
- Anti-debug checks on release builds

**We do not ship an aggressive client-side anti-cheat.** It generates false positives, angers legitimate players, and provides little real protection against a determined attacker who can simply run the game on real hardware.

---

## 9. Incident response

```
1. DETECT   — alert from anomaly monitors or a player report
2. CONTAIN  — feature flag off, rate-limit the vector, or block the endpoint
3. ASSESS   — query currency_audit to size the damage
4. FIX      — patch server-side (no client release needed for most vectors)
5. REMEDIATE— roll back illegitimate gains; compensate affected players
6. POST-MORTEM — write it up in docs/incidents/YYYY-MM-DD.md
```

**Feature flags exist for this reason.** Any system can be switched off server-side without a client update. An exploited event, arena format, or voice feature can be disabled in seconds.

`currency_audit` (`15 §7`) is what makes step 3 and 5 possible. Without it, an exploit means guessing at who gained what.

---

## 10. Acceptance criteria (Phase 9)
- [ ] Every mutating endpoint validated server-side; no endpoint reads a client-computed result
- [ ] 100% of runner submissions re-simulated
- [ ] ★ Arena intent validation complete per `16 §6`
- [ ] Purchase receipts validated with Google before any grant
- [ ] Refresh-token rotation with reuse detection
- [ ] Rate limits on every endpoint
- [ ] Admin panel unreachable from the public internet; TOTP + audit log verified
- [ ] `currency_audit` row written for every currency change
- [ ] Bot-detection signals collected and scored
- [ ] Feature flags exist for every major system
- [ ] Penetration self-test: attempt each threat in §1 and document the result
- [ ] No secrets in the repository (verified with a secret-scanning tool in CI)

---

## Next
- `22-legal-compliance.md`
- `24-devops-deployment.md`

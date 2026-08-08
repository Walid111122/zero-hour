# 24 — DevOps & Deployment

> One VM, Docker Compose, GitHub Actions. **$0/month** until scale demands otherwise.

---

## 1. Environments

| Env | Where | Purpose |
|---|---|---|
| **Local** | Dev machine, Docker Compose | Day-to-day development |
| **Staging** | Same free VM, separate compose project + DB | Pre-release verification |
| **Production** | Free VM | Live |

Staging shares the box but never the database. It exists so migrations and event definitions can be verified against real infrastructure before they touch player data.

---

## 2. The VM

**Oracle Cloud Always Free — ARM Ampere A1:** 4 OCPU, 24 GB RAM, 200 GB storage, 10 TB/month egress. Ubuntu 24.04 LTS.

Setup, in order:
1. SSH key auth only, password auth disabled, root login disabled
2. `ufw`: allow 22 (restricted to your IP), 80, 443, 7880–7881 (LiveKit), 3478/UDP + 49152–65535/UDP (coturn)
3. `fail2ban` on SSH
4. Unattended security upgrades enabled
5. Docker + Compose plugin
6. Swap file (4 GB) as a safety margin
7. Cloudflare in front for TLS termination, caching, and DDoS absorption (free tier)

**Nothing else is exposed.** Postgres, Redis, and the admin panel bind to the Docker network only — never to a public interface. The admin panel is reached through an SSH tunnel (`20 §7`).

---

## 3. Docker Compose services

```yaml
services:
  nginx:      # TLS, rate limiting, routes to app; ports 80/443
  app:        # ZeroHour.Server (ASP.NET Core 10)
  admin:      # ZeroHour.Admin — internal network ONLY, no published port
  postgres:   # 16, volume-backed, internal only
  redis:      # 7, appendonly, internal only
  livekit:    # ★ voice SFU, 7880/7881
  coturn:     # ★ TURN relay for restrictive NATs
  translate:  # LibreTranslate, internal only
  posthog:    # optional self-hosted analytics
```

Resource limits are set per container so one runaway service cannot starve the others. The app gets the most CPU; LiveKit gets the most network.

---

## 4. Deployment

```
git push origin main
   ↓ GitHub Actions
   1. Test (sim → server → determinism)      — a failure stops here
   2. Build Docker image (multi-arch, ARM64)
   3. Push to GitHub Container Registry
   4. SSH to VM → docker compose pull → up -d --no-deps app
   5. Health check /health
   6. Roll back to the previous image tag if unhealthy
```

**Zero-downtime is not attempted at this scale.** A ~5 second restart is acceptable and far simpler than blue/green on a single box. The client handles it gracefully: SignalR reconnects, REST calls retry with backoff, and any in-flight ★ arena match is voided with entries refunded (`16 §8`).

### Migrations
- Applied by the app on startup, inside a transaction
- Every migration must be **backwards-compatible for one version** so a rollback doesn't corrupt data
- Destructive changes are split across two releases: add the new column, deploy, backfill, deploy, then drop the old column in a later release

---

## 5. Backups

```
Nightly 03:00 UTC:
  pg_dump --format=custom  →  gzip  →  encrypt (age)
  → upload to Backblaze B2 (10 GB free tier)
  Retention: 7 daily, 4 weekly, 3 monthly
Redis: appendonly + hourly RDB snapshot to local disk (rebuildable, low priority)
```

**Monthly restore drill, documented.** Restore the latest dump into a scratch database, run migrations, boot the app against it, and confirm a player state loads. A backup that has never been restored is a hope, not a backup — and finding out during an incident is the worst possible time.

---

## 6. Monitoring

| Layer | Tool |
|---|---|
| Uptime | UptimeRobot free tier, 5-min checks on `/health` |
| Metrics | Prometheus + Grafana (containers), or simple `/metrics` scraping |
| Logs | Serilog → files with rotation; `docker compose logs` for ad-hoc |
| Errors | Sentry free tier (server + client) |
| Product analytics | PostHog |
| Alerts | Email + push per `21 §6` |

Key server metrics: request rate, p50/p95 latency, error rate, DB connection pool usage, Redis hit rate, **★ arena tick overruns**, **★ voice room count and bandwidth**, CPU/RAM/disk.

`/health` checks the app, Postgres, and Redis. `/health/deep` also checks LiveKit and LibreTranslate, and is used by the deploy gate.

---

## 7. Secrets

- Stored as environment variables on the VM in a `.env` file with `600` permissions, owned by the deploy user
- **Never in git.** `.gitignore` covers `.env*`, and CI runs a secret scanner on every push
- GitHub Actions secrets for the deploy key and registry credentials
- Rotation: JWT signing key, DB password, and LiveKit API key rotated on any suspicion of exposure and at least annually
- Rotation procedure documented so it can be done under pressure without guessing

---

## 8. Client release process

```
1. Version bump (semver + build number)
2. CI: Unity build → AAB, signed with the upload key
3. Upload to Play Console internal testing
4. Smoke test on the physical device matrix (23 §7)
5. Promote → closed testing (~50 users)
6. Watch crash rate and D1 for 48 h
7. Promote → open testing / production with a staged rollout: 5% → 20% → 50% → 100%
8. Halt the rollout if crash rate > 1% or a P0 appears
```

**Keystore handling:** the upload keystore is backed up in two independent encrypted locations. Losing it means you can never update the app again under the same listing. Play App Signing protects the app signing key, but the upload key is still yours to lose.

---

## 9. Server-side release levers (no client build needed)

This is what lets a solo developer run live-ops:

| Lever | Mechanism |
|---|---|
| Balance changes | CSV → config reload (`14 §8`) |
| New events | JSON definition + server-driven UI (`09 §1`) |
| Feature flags | Toggle any system off instantly |
| Event art | Remote Addressables group (`18 §7`) |
| Localisation fixes | Server-delivered string overrides |
| Exploit containment | Endpoint block or flag off (`20 §9`) |

**A client release should be needed only for new code**, never for tuning, content, or incident response.

---

## 10. Runbooks

Short, written before they're needed, kept in `docs/runbooks/`:

| Scenario | Action |
|---|---|
| Server down | Check compose status → logs → restart app → escalate to full stack restart |
| DB out of disk | Vacuum, drop old battle reports/chat, resize volume |
| Redis full | It's a cache — flush it, the game degrades gracefully |
| ★ Arena tick overruns rising | Lower the concurrent match cap, queue new matches |
| ★ Voice failures | Restart LiveKit; verify coturn; check UDP port reachability |
| Exploit discovered | Feature flag off → assess via `currency_audit` → patch → remediate |
| Bad deploy | Roll back to the previous image tag |
| Bad migration | Restore from backup to staging first, verify, then decide |
| Data loss | Restore drill procedure; publish an honest status update to players |

---

## 11. Cost checkpoints

| Trigger | Action |
|---|---|
| CPU > 70% sustained | Move Postgres to its own VM (~$20/mo) |
| Egress > 7 TB/month | Add Cloudflare caching for assets; review ★ voice bitrate |
| DAU > 5,000 | Follow the scaling path in `14 §9` |
| Revenue > $1,000/month | Stop optimising for free tiers; buy the right infrastructure |

That last row is the important one. Free-tier engineering is correct at zero revenue and a waste of time once money is coming in.

---

## Next
- `25-launch-ua-plan.md`
- `26-ROADMAP.md`

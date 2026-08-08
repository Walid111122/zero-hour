# Phase 0 — Foundation & Tooling

> **Goal:** a repo that builds, a Unity project that opens clean, and a working Cline ↔ Unity feedback loop.
> **Est:** 2–3 weeks · **Docs:** `14`, `17`, `24`, `27`, `28`

**Gate to Phase 1:** Unity opens with zero errors · Bridge writes console logs to file · `dotnet build` succeeds on the full solution · git repo initialised with LFS · CI green

---

## 0.1 Repository

- [ ] `git init`, initial commit
- [ ] `.gitignore` — Unity (`Library/`, `Temp/`, `Logs/`, `Build/`, `UserSettings/`, `*.csproj`, `*.sln.user`) + .NET (`bin/`, `obj/`) + `bridge/` + `.env*`
- [ ] `.gitattributes` — LFS for `*.png *.jpg *.psd *.fbx *.wav *.ogg *.mp3 *.ttf`, plus `* text=auto eol=lf`
- [ ] `README.md` — one-paragraph project summary, links to `docs/00-README-INDEX.md`
- [ ] `LICENSE` decision (proprietary; keep third-party attributions in `docs/ATTRIBUTION.md`)
- [ ] `docs/ATTRIBUTION.md` created empty with the table header (`18 §2`)
- [ ] Branch policy: work on `main` solo, but never force-push

## 0.2 Solution & shared Sim

- [ ] `ZeroHour.sln` at repo root
- [ ] `shared/ZeroHour.Sim/` — **netstandard2.1**, `#nullable enable`, no `UnityEngine`, **no `float`/`double`**
- [ ] `Fixed` fixed-point type (Q32.32 or Q16.16) with `+ - * /`, `Sqrt`, `Min/Max`, `Lerp`
- [ ] `DetRandom` — xorshift or PCG, explicit seed, no `System.Random`
- [ ] `Hash` — stable FNV/xxHash over state for determinism fixtures
- [ ] Analyzer or unit test that **fails the build if `float`/`double`/`System.Random`/`DateTime.Now` appear in `Sim`**
- [ ] `shared/ZeroHour.Sim.Tests/` — xUnit; fixed-point arithmetic tests; RNG reproducibility tests
- [ ] `dotnet test` passes in under 1 second

The float ban needs to be enforced by tooling, not discipline. A single `float` that slips into the sim produces a divergence that is extremely painful to find months later (`23 §3`).

## 0.3 Unity client

- [ ] `client/` Unity project on **6000.5.6f1**, URP template
- [ ] Folder structure per `17 §1` (`Assets/_Project/...`)
- [ ] Packages installed per `17 §6` — **you do this in Package Manager**
- [ ] Assembly definitions per `17 §2` with the dependency rules enforced
- [ ] `ZeroHour.Sim.dll` referenced from `Assets/Plugins/`
- [ ] `tools/scripts/build-sim.ps1` — builds `shared/Sim` and copies the DLL into Unity
- [ ] `Boot.unity` + `Main.unity` scenes, additive load per `17 §4`
- [ ] `Bootstrap.cs` composition root + `ServiceContainer` + `ServiceLocator`
- [ ] Stub services: `IClock`, `IConfigService`, `ISaveService`, `IEventBus`, `IAudioService`, `IApiClient`
- [ ] Player settings: portrait, IL2CPP, ARM64, min API 24, target API 35
- [ ] Three quality tiers created (`18 §8`)
- [ ] Project opens with **zero console errors**

## 0.4 Cline ↔ Unity Bridge

Per `28 §2`. Editor-only assembly, file-based protocol, gitignored `bridge/` folder.

- [ ] `ZeroHour.Bridge` asmdef — **Editor platform only**
- [ ] `BridgeWatcher` polling `bridge/request.json` on `EditorApplication.update`
- [ ] Console capture → `bridge/console.log` (rolling, with severity + stack)
- [ ] Response writer → `bridge/response.json`
- [ ] Command: `compile` — returns errors/warnings with file:line
- [ ] Command: `refresh`
- [ ] Command: `enter_play` / `exit_play` with exception capture
- [ ] Command: `run_tests` (edit + play mode, per-test results)
- [ ] Command: `get_logs`
- [ ] Command: `screenshot` → PNG of the Game view
- [ ] Command: `scene_dump` → hierarchy as JSON
- [ ] Command: `build_webgl` / `build_android`
- [ ] Command: `generate_so` — CSV → ScriptableObject instances
- [ ] Fixed command allowlist, **no arbitrary code execution**
- [ ] Verified excluded from player builds (check a build's assembly list)

## 0.5 Server skeleton

- [ ] `server/ZeroHour.Server/` — ASP.NET Core 10, minimal API
- [ ] `GET /health` → app status; `GET /health/deep` → + dependencies
- [ ] SignalR hub with an echo method (proves the realtime path)
- [ ] Serilog structured logging, no PII
- [ ] `server/ZeroHour.Tests/` — xUnit + Testcontainers
- [ ] EF Core with SQLite for local dev, Postgres provider ready
- [ ] First migration: `players`, `player_states` (`15 §2`)
- [ ] `docker-compose.yml` for local: app + postgres + redis
- [ ] Config via environment variables; `.env.example` committed, `.env` gitignored

## 0.6 CI (GitHub Actions)

- [ ] `sim` job — `dotnet test shared/` — **must stay under 1 minute**
- [ ] `server` job — `dotnet test server/` with Testcontainers
- [ ] `dotnet list package --vulnerable` fails the build on a high severity
- [ ] Secret scan on every push
- [ ] `unity` job — nightly, GameCI, edit-mode tests + Android build artifact
- [ ] Status badge in `README.md`

## 0.7 Your manual steps (`27 §2`)

- [ ] Install Unity Hub + **6000.5.6f1** with Android + WebGL modules
- [ ] Sign in to Unity (Personal licence)
- [ ] Open `f:\last war build\client`, wait out the first import
- [ ] Install the packages Cline lists
- [ ] Run `tools\scripts\build-sim.ps1` once
- [ ] Press Play on `Boot.unity`, confirm no errors
- [ ] Report the console output back

---

## Gate checklist

- [ ] `dotnet build` clean on the whole solution
- [ ] `dotnet test` green, sim tests under 1 s
- [ ] Unity opens with zero errors and enters play mode
- [ ] Bridge round-trip verified: Cline issues `compile`, reads the result
- [ ] Bridge `screenshot` produces a viewable PNG
- [ ] Docker Compose brings up app + postgres + redis locally
- [ ] `GET /health` returns healthy
- [ ] CI green on a fresh push
- [ ] Git LFS tracking confirmed (`git lfs ls-files`)

**Do not start Phase 1 until every box above is ticked.** Phase 0 is unglamorous, and the temptation to skip ahead to the fun part is strongest here. The bridge in particular pays for itself within days — without it, every Phase 1 iteration costs a manual round-trip.

→ Next: [phase-1-mvp.md](phase-1-mvp.md)

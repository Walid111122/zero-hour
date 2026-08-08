# Phase 0 — Foundation & Tooling

> **Goal:** a repo that builds, a Unity project that opens clean, and a working Cline ↔ Unity feedback loop.
> **Est:** 2–3 weeks · **Docs:** `14`, `17`, `24`, `27`, `28`

**Gate to Phase 1:** Unity opens with zero errors · Bridge writes console logs to file · `dotnet build` succeeds on the full solution · git repo initialised with LFS · CI green

---

## 0.1 Repository

- [x] `git init`, initial commit — `39da4db`, LFS installed
- [x] `.gitignore` — Unity (`Library/`, `Temp/`, `Logs/`, `Build/`, `UserSettings/`, `*.csproj`, `*.sln.user`) + .NET (`bin/`, `obj/`) + `bridge/` + `.env*`
- [x] `.gitattributes` — LFS for `*.png *.jpg *.psd *.fbx *.wav *.ogg *.mp3 *.ttf`, plus `* text=auto eol=lf`
- [x] `README.md` — project summary, layout table, and the no-float rule stated up front
- [ ] `LICENSE` decision (proprietary; keep third-party attributions in `docs/ATTRIBUTION.md`)
- [ ] `docs/ATTRIBUTION.md` created empty with the table header (`18 §2`)
- [ ] Branch policy: work on `main` solo, but never force-push

## 0.2 Solution & shared Sim

- [x] `ZeroHour.slnx` at repo root — .NET 10 emits the XML solution format, not `.sln`
- [x] `shared/ZeroHour.Sim/` — **netstandard2.1**, `#nullable enable`, no `UnityEngine`, **no `float`/`double`**
- [x] `Fixed` fixed-point type (**Q32.32**) with `+ - * /`, `Sqrt`, `Min/Max`, `Lerp`, `Clamp`, `Parse`
- [x] `DetRandom` — xorshift128+ seeded via SplitMix64, explicit seed, serialisable state, no `System.Random`
- [x] `Hash` — stable FNV-1a 64 over state for determinism fixtures
- [x] Unit tests that **fail the build if `float`/`double`/`System.Random`/`DateTime` appear in `Sim`** — `DeterminismGuardTests`
- [x] `shared/ZeroHour.Sim.Tests/` — xUnit; fixed-point arithmetic tests; RNG reproducibility tests
- [x] `dotnet test` passes in under 1 second — **40 tests, 52 ms, zero warnings**

The float ban needs to be enforced by tooling, not discipline. A single `float` that slips into the sim produces a divergence that is extremely painful to find months later (`23 §3`).

**Implementation notes (2026-08-08):**
- `Fixed` multiply/divide use a hand-rolled 64×64→128-bit intermediate rather than `Int128`,
  because `Int128` does not exist on netstandard2.1 and Unity must consume this assembly.
- `Fixed.Divide` shifts the dividend inside the 128-bit path, so `FromFraction(1, 1_000_000_000)`
  works without overflowing the denominator.
- `Sqrt` is a binary search on the result, never a hardware `sqrt` instruction.
- `Hash.Add(string)` length-prefixes and folds UTF-16 units, so `"ab"+"c"` cannot collide
  with `"a"+"bc"`. The pinned vector `Hash.Of("a") == 0x2BC75A111F39F5D5` guards the algorithm;
  if it ever changes, every recorded fixture hash must be regenerated deliberately.

## 0.3 Unity client

- [x] `client/` Unity project on **6000.5.6f1** — created via `-createProject`, URP package added
- [~] Folder structure per `17 §1` (`Assets/_Project/...`) — `Code/Editor/Bridge` in place
- [x] Packages installed per `17 §6` — scripted in `Assets/Editor/PackageSetup.cs`, no manual clicking:
      URP 17.5.0 · Input System 1.20.0 · Addressables 4.0.1 · Test Framework 1.7.0 ·
      Newtonsoft Json 3.2.2 · uGUI 2.5.0. Localization, Mobile Notifications, WebRTC and
      Billing are deferred to the phase that needs them — unused packages still cost import time.
- [ ] Assembly definitions per `17 §2` with the dependency rules enforced
- [x] `ZeroHour.Sim.dll` referenced from `Assets/Plugins/ZeroHour.Sim/`
- [x] `tools/scripts/build-sim.ps1` — builds `shared/Sim`, **runs the determinism suite, and
      refuses to copy the DLL if it fails**, then copies into Unity
- [ ] `Boot.unity` + `Main.unity` scenes, additive load per `17 §4`
- [ ] `Bootstrap.cs` composition root + `ServiceContainer` + `ServiceLocator`
- [ ] Stub services: `IClock`, `IConfigService`, `ISaveService`, `IEventBus`, `IAudioService`, `IApiClient`
- [ ] Player settings: portrait, IL2CPP, ARM64, min API 24, target API 35
- [ ] Three quality tiers created (`18 §8`)
- [ ] Project opens with **zero console errors**

## 0.4 Cline ↔ Unity Bridge

Per `28 §2`. Editor-only assembly, file-based protocol, gitignored `bridge/` folder.

- [x] `ZeroHour.Bridge` asmdef — **Editor platform only** (`includePlatforms: ["Editor"]`)
- [x] `BridgeWatcher` polling `bridge/request.json` on `EditorApplication.update` (0.5 s)
- [x] Console capture → `bridge/console.log` (rolling at 4 MB, severity + stack)
- [x] Response writer → `bridge/response.json` (temp-file + move, so no partial reads)
- [x] Command: `compile` — returns errors/warnings, survives the domain reload
- [x] Command: `refresh`
- [x] Command: `enter_play` / `exit_play` with error count
- [ ] Command: `run_tests` (edit + play mode, per-test results)
- [x] Command: `get_logs`
- [x] Command: `screenshot` → PNG of the Game view
- [x] Command: `scene_dump` → hierarchy as JSON (depth-capped at 8)
- [ ] Command: `build_webgl` / `build_android`
- [ ] Command: `generate_so` — CSV → ScriptableObject instances
- [x] Fixed command allowlist, **no arbitrary code execution**
- [x] Verified excluded from player builds — editor-only asmdef; `bridge/` is gitignored
- [x] **Round-trip verified:** `{"command":"ping"}` → `{"ok":true,"message":"pong"}` in ~10 s

**Why a file protocol rather than a socket:** a `compile` command triggers a domain reload that
would drop an open socket mid-command. Files survive the reload — the pending request id parks
in `SessionState` and the response is written once the editor comes back. It also leaves an
inspectable transcript on disk when something misbehaves.

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

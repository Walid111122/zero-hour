# 27 — Unity Editor Guide (What You Do By Hand)

> Cline writes the code. A small number of things still need a human in the Unity Editor.
> This document is the complete list, so nothing is a surprise later.

---

## 1. The division of labour

| Cline can do | You do in the Editor |
|---|---|
| All C# scripts | Install packages via Package Manager |
| `.asmdef` assembly definitions | Sign in to your Unity account / licence |
| ScriptableObject **classes** | Create ScriptableObject **instances** (or run a Cline-written menu item) |
| Scene files via script | Visually arrange scenes, place cameras |
| Prefab creation via editor scripts | Visual prefab tweaking, drag-and-drop wiring |
| CSV/JSON data | Import art and audio, set import settings |
| Build automation scripts | Android keystore creation, Play Console upload |
| URP asset generation | Confirm quality tier assignments look right |
| Test code | Press Play and tell Cline how it *feels* |

**The pattern:** Cline handles anything text-based and deterministic. You handle licences, credentials, visual judgement, and the one thing no tool can do — deciding whether the game is fun.

---

## 2. One-time setup (Phase 0)

### 2.1 Install Unity
- Unity Hub → install **6000.5.6f1**
- Modules: **Android Build Support** (includes SDK/NDK/JDK), **WebGL Build Support**
- Sign in with a Unity account; Personal licence is fine under $200k revenue

### 2.2 Open the project
`Unity Hub → Add → f:\last war build\client`

First open takes several minutes while the library imports. This is normal.

### 2.3 Install packages
`Window → Package Manager`, install per `17 §6`. All are free Unity registry packages.

### 2.4 Verify the Sim DLL
`Assets/Plugins/ZeroHour.Sim.dll` should exist. If it doesn't, run:
```
tools\scripts\build-sim.ps1
```
That script builds `shared/ZeroHour.Sim` and copies the DLL into the Unity project. Run it any time Cline changes sim code.

### 2.5 Project settings
Most are set by a Cline-written script, but confirm visually:
- Player → Orientation: **Portrait**
- Player → Scripting Backend: **IL2CPP**, Target: **ARM64**
- Player → Minimum API: **24**, Target: **35**
- Quality: three tiers present
- Graphics: URP asset assigned

---

## 3. Recurring editor tasks

### After Cline changes sim code
```
tools\scripts\build-sim.ps1
```
then return to Unity and let it recompile. Cline will tell you when this is needed.

### After Cline adds scripts
Unity auto-recompiles on focus. Watch the Console. If there are errors, paste them to Cline — full text, not a summary.

### Creating data instances
Cline writes menu items for this, e.g.:
```
Tools → Zero Hour → Generate Balance ScriptableObjects
Tools → Zero Hour → Import Localization CSV
Tools → Zero Hour → Create Test Player Save
```
Prefer these over hand-creating assets — they're repeatable and produce consistent results.

### Prefab wiring
Cline can create prefabs by script, but visual polish (exact positions, particle tuning, UI spacing) is faster by hand. When you rearrange something Cline built, mention it so the generator script gets updated to match. Otherwise the next regeneration undoes your work.

---

## 4. Play-mode testing

The most valuable thing you do.

```
1. Open Scenes/Boot.unity
2. Press Play
3. Watch the Console for errors and warnings
4. Play the actual game
5. Report back: what broke, what felt wrong, what felt good
```

**"It feels sluggish" is more useful to Cline than "there's a bug."** Timing, weight, and responsiveness are things only a human sitting with the game can judge, and they're exactly what determines whether Phase 1's gate passes.

Useful shortcuts: `Ctrl+P` play/pause, `Ctrl+Shift+C` console, `Ctrl+Shift+P` pause, and the Stats overlay in the Game view for a quick FPS/draw-call read.

---

## 5. Builds

### Android (AAB)
```
Tools → Zero Hour → Build Android (Development)
Tools → Zero Hour → Build Android (Release)
```
First release build requires a keystore:
```
Player Settings → Publishing Settings → Keystore Manager → Create New
```
**Back the keystore up in two encrypted locations immediately.** Losing it means you can never update the app under the same listing (`24 §8`).

### WebGL (for sharing playtests)
```
Tools → Zero Hour → Build WebGL
```
Produces a folder you can host anywhere static — GitHub Pages, Netlify free tier. This is how you get a build in front of five strangers for the Phase 1 gate without asking anyone to install an APK. It's worth keeping working for that reason alone.

---

## 6. Common problems

| Symptom | Fix |
|---|---|
| "The type or namespace `ZeroHour.Sim` could not be found" | Run `build-sim.ps1`, then let Unity recompile |
| Scripts not compiling after a Cline edit | Click into the Unity window to trigger a refresh; check for a compile error blocking everything |
| Pink materials | URP asset not assigned, or a Built-in RP material — reimport or switch the shader |
| Play mode does nothing | Wrong scene open. Start from `Boot.unity` |
| Android build fails on SDK | Unity Hub → add Android Build Support module |
| Very slow editor | Disable auto-refresh while working; close the Profiler when not using it |
| Prefab changes lost | You edited an instance, not the prefab. Use "Overrides → Apply All" |

When something breaks, **paste the full console text to Cline**. Truncated errors cost a round-trip.

---

## 7. What to never do

- Don't edit files under `Assets/Plugins/ZeroHour.Sim.dll` — it's generated
- Don't hand-edit `.meta` files
- Don't commit `Library/`, `Temp/`, `Logs/`, or `Build/` (the `.gitignore` covers these)
- Don't upgrade the Unity version mid-phase; version changes are their own task
- Don't add a paid asset store package without saying so — Cline can't see it and will write code that doesn't know it exists

---

## Next
- `28-cline-workflow.md` — how to actually drive this collaboration
- `checklists/phase-0-foundation.md`

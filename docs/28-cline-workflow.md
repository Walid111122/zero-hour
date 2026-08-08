# 28 — Cline Workflow & The Unity Bridge

> How to actually drive this collaboration over 11 phases, and the tooling that lets me see whether the code I wrote compiles.

---

## 1. The core problem

I can write C# all day, but Unity is a closed editor. Without tooling I can't tell whether my code compiles, whether play mode throws, or what the console says. That turns every change into: write code → ask you to check → wait → paste errors → repeat.

**The bridge closes that loop.** It's a small editor package that lets me issue commands to a running Unity instance and read the results back.

---

## 2. Bridge architecture

```
Cline ──writes──▶ bridge/request.json
                        │
Unity Editor (ZeroHour.Bridge, editor-only assembly)
   EditorApplication.update polls for request.json
                        │
                        ▼
   Executes: compile / play / test / build / screenshot / logs
                        │
                        ▼
Cline ──reads──▶ bridge/response.json  +  bridge/console.log
```

File-based on purpose: no ports, no permissions prompts, no protocol to debug. Just JSON files in a gitignored folder that both sides can read and write.

### Commands
| Command | Returns |
|---|---|
| `compile` | Compile errors and warnings with file/line |
| `refresh` | Reimports assets, then compiles |
| `enter_play` / `exit_play` | Play-mode entry result plus any exceptions |
| `run_tests` | Edit/play-mode test results, per-test |
| `build_webgl` / `build_android` | Build success/failure and the output path |
| `screenshot` | A PNG of the Game view so I can see the layout |
| `get_logs` | The last N console lines |
| `scene_dump` | The hierarchy of the open scene as JSON |
| `create_prefab` | Builds a prefab from a spec |
| `generate_so` | Creates ScriptableObject instances from CSV |

`screenshot` and `scene_dump` are the two that matter most for UI work — they let me verify what I built actually looks right rather than guessing from code.

### Safety
The bridge is an **editor-only assembly**, excluded from all player builds. It reads only from the project's `bridge/` folder, executes a fixed command set with no arbitrary code execution, and is gitignored. It cannot ship to players.

---

## 3. My workflow for each feature

```
1. Read the relevant doc section (they're the spec, and they're detailed for this reason)
2. Write the Sim logic first, in shared/ — pure, deterministic, no Unity
3. Write unit tests, run: dotnet test        ← fast, no Unity needed
4. Write the server endpoint + integration test
5. Write the Unity view layer
6. Bridge: compile → fix errors → repeat until clean
7. Bridge: run_tests
8. Bridge: enter_play, read the console
9. Bridge: screenshot to verify the layout
10. Hand it to you: "play this, tell me how it feels"
```

**Steps 2–4 happen entirely without Unity.** That's the payoff of the shared-sim architecture (`14 §2`) — most of the real logic in this game can be developed and tested at full speed in plain .NET.

---

## 4. What I need from you

| When | What |
|---|---|
| Compile errors after a bridge run | Nothing — I can read them myself |
| Play mode misbehaving | The full console text, not a summary |
| Something feels wrong | Describe the *feel*: sluggish, floaty, unclear, unfair |
| A new package needed | Install it; I'll tell you exactly which one |
| Credentials, keystores, accounts | Only you can do these |
| Phase gate decisions | Your honest judgement — I can't tell if it's fun |

**The gate calls are yours alone.** I can verify that a build compiles, passes tests, and hits its frame budget. I cannot tell you whether the runner loop is fun, and I won't pretend otherwise. That question decides the project.

---

## 5. How to instruct me effectively

**Good:**
> "Implement the resource accrual from `04 §4` including the 8-hour overflow cap. Sim first, unit tests, then wire the base HUD."

**Less good:**
> "Add resources."

The docs exist so instructions can be short *and* precise: point me at a section and I have the full spec. When you want something different from what's written, say so explicitly — I'll follow the doc otherwise, and I'll flag the mismatch rather than silently picking.

### Useful habits
- One feature per task. Long tasks accumulate context and drift
- Reference doc sections by number
- Tell me when you've changed something in the editor by hand, so my generator scripts don't overwrite it
- If I've gone the wrong direction twice, say so — I'll change approach rather than keep tweaking

---

## 6. Phase discipline

I'll work through `checklists/phase-N-*.md` in order and keep the boxes updated. I will not start Phase N+1 work while Phase N's gate is unanswered, even if it seems more interesting — that's the mechanism protecting a 14-month solo project from scope creep.

If I think a doc is wrong (a balance number that breaks, an architecture choice that won't hold), I'll say so and explain why rather than implementing something I believe is broken. The docs are a plan, not scripture.

---

## 7. Session hygiene

- I keep `checklists/` updated as the persistent memory across sessions — it's how a new session knows where we are
- Long-running work gets committed at meaningful checkpoints, only when you ask
- I'll never force-push, reset --hard, or rewrite history without asking
- I'll flag anything that touches secrets, production, or player data before doing it

---

## 8. Realistic expectations

What I'm good at here: sim logic, server endpoints, netcode structure, tests, tooling, boilerplate, refactors, and keeping 29 documents' worth of decisions consistent across a year of work.

What I'm not: a game designer with taste, an artist, or a substitute for playing the thing. The ★ arena feel, the runner's timing, whether an offer seems predatory — those need you.

The division works because it plays to both sides. You bring judgement and direction; I bring throughput and consistency. Over 11 phases, consistency is worth a lot: the reason this project can plausibly ship is that the plan is written down and I'll still be following it in month twelve.

---

## Next
- `checklists/phase-0-foundation.md` — start here
- `27-unity-editor-guide.md` — your side of the work

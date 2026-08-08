# 19 — UX & UI System

> Portrait, one-handed, thumb-first. Every core action reachable by a right thumb without shifting grip.

---

## 1. Screen architecture

```
Persistent HUD (always visible)
├── Top bar    : resources, diamonds, power, settings
├── Layer tabs : [Runner] [Base] [World] [Alliance]        ← bottom, thumb zone
├── Side rails : events (left), shop/mail (right)
└── ★ Voice widget : floating, draggable, survives scene changes
```

### The thumb zone rule
```
┌─────────────────┐
│  INFO ONLY      │  top third: display, never interactive
├─────────────────┤
│  CONTENT        │  middle: scroll/pan, taps on world objects
├─────────────────┤
│  ACTIONS ✋     │  bottom third: every button that matters
└─────────────────┘
```
Confirm buttons, tab bar, ★ arena skill buttons, and ★ voice PTT all live in the bottom third. Destructive actions get extra spacing so a misplaced thumb doesn't cost troops.

---

## 2. Navigation

- **Max 3 taps** from anywhere to any core action
- Hardware back / swipe-back closes the top-most panel, never the app (except at root, where it prompts)
- Modals stack, and the stack is popped in order
- Deep links from push notifications and chat open the exact target screen

---

## 3. The red-dot system

A hierarchical notification tree — the single strongest driver of session frequency in this genre.

```
Root
├── Base        → building ready, help available, free speedup
├── Alliance    → help requests, gifts unclaimed, ★ arena invite
├── Events      → claimable milestone, phase starting
├── Mail        → unread with attachments
├── Heroes      → free pull, upgradable hero, unassigned slot
└── ★ Arena     → challenge received, ladder reward, match starting
```

**Rules:**
- A dot means **a free action is available now**, never "there is content here"
- Dots propagate up; clearing a leaf clears ancestors automatically
- A dot that cannot be cleared by any action is a bug
- Paid actions never produce a dot. Ever.

That last rule is the difference between a helpful signal and a manipulative one. If a red dot ever means "buy something," players stop trusting all of them and the system dies.

---

## 4. Feedback & juice

Every interaction gets: **visual + audio + haptic** response within 100 ms.

| Interaction | Feedback |
|---|---|
| Button tap | Scale 0.95 → 1.0, click SFX, light haptic |
| Resource collect | Coins fly to the counter, count rolls up, chime |
| Level up | Flash, particle burst, fanfare, medium haptic |
| Damage | Number pops, unit flashes, screen shake (scaled) |
| Reward | Chest opens, staged reveal, rarity-scaled fanfare |
| Error | Shake, red flash, distinct error SFX |
| ★ Speaking | Animated ring on the speaker's avatar |
| ★ Arena kill | Kill-feed sting + brief slow-mo on match point |

**Screen shake is capped and user-disableable.** Accessibility, and some players find it nauseating.

---

## 5. Onboarding

The most important UI work in the project. Detailed flow in `03 §4`.

| Principle | Application |
|---|---|
| Play in < 10 s | Cold start → shooting. No logo gauntlet, no login wall |
| Show, never tell | Highlight + arrow + one short line. No text walls |
| One thing at a time | A single tutorial objective on screen, ever |
| Never block twice | A mechanic is tutorialised once |
| Skippable after basics | Returning players can skip from step 5 |
| Delayed account | Guest play immediately; account prompt at HQ 5 |

**Tutorial is data-driven** (`tutorial_steps.csv`): step id, trigger, highlight target, copy key, completion condition. This allows tuning FTUE from analytics without a client build — and FTUE is the thing you will tune most.

---

## 6. Accessibility

| Feature | Implementation |
|---|---|
| Colourblind modes | Protanopia / deuteranopia / tritanopia palettes; **never colour alone** to convey state |
| Text scaling | 100% / 125% / 150% |
| Reduced motion | Disables shake, parallax, heavy particles |
| ★ Voice alternative | Full text chat parity — voice is never required to participate |
| Haptics toggle | On/off |
| Hold-instead-of-tap | For players who struggle with precise taps |
| Minimum touch target | **44×44 dp**, no exceptions |
| Contrast | WCAG AA on all text |

The ★ voice parity rule matters: deaf and hard-of-hearing players, and players who simply can't speak aloud, must be able to fully participate in rallies and arena matches through text.

---

## 7. Performance rules for UI

- **Canvas splitting:** static and dynamic elements on separate canvases (a single dirty element rebuilds its whole canvas)
- Pool all list items; never instantiate per-frame
- `TextMeshPro` everywhere; legacy `Text` is banned
- Disable raycast targets on non-interactive graphics
- Long lists use recycling scroll views
- **Zero allocation per frame** in HUD update paths — cache references, use `StringBuilder`, avoid `string.Format` in `Update`

---

## 8. Key screens

| Screen | Notes |
|---|---|
| **Base** | 3/4 view, tap building → radial action menu; build queue as a top strip |
| **World** | Pan/pinch map, tap tile → context sheet; march bar along the bottom |
| **Runner** | Zero UI except pause + HP; the game *is* the screen |
| **★ Arena lobby** | Room list, format/map pickers, roster, big Ready button, voice join |
| **★ Arena match** | HUD per `11 §7.2` — skills bottom-right, PTT bottom-left |
| **Alliance** | Tabs: Members / Help / Tech / Gifts / Chat / ★ Voice / ★ Arena |
| **Event** | Server-driven templates (`14 §7`) |
| **Shop** | Clear pricing, no dark patterns, one-tap dismiss on offers |

---

## Next
- `18-art-audio-pipeline.md`
- `23-qa-testing.md`

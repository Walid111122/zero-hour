# 18 — Art & Audio Pipeline

> Constraint: **no artist, no budget.** The pipeline is designed around that, not in spite of it.

---

## 1. Art direction

**Post-apocalyptic military, clean and readable.** Not gritty realism — readability at phone size beats fidelity every time.

| Element | Direction |
|---|---|
| Palette | Desaturated base (grey/olive/rust) + high-saturation accents for interactive elements |
| Style | Semi-stylised low-poly 3D units, 2D UI |
| Silhouettes | Each troop type must be identifiable at 32 px. Tank = wide/low, Air = elevated, Missile = tall/thin |
| Lighting | Baked, single directional light. No realtime shadows on units |
| Camera | Fixed-angle 3/4 for base, top-down for world, 3/4 for ★ arena |

**The readability rule is load-bearing.** In ★ arena, a player must tell friend from foe and tank from missile in a fraction of a second on a 6-inch screen. Team colour rims + distinct silhouettes, verified by squinting at the screen from arm's length.

---

## 2. Asset sourcing (in priority order)

| Source | Use | Licence |
|---|---|---|
| **Kenney.nl** | Placeholder everything, UI, props | CC0 |
| **Quaternius** | Low-poly military models | CC0 |
| **Poly Pizza / OpenGameArt** | Props, environment | CC0 / CC-BY |
| **Blender** (self-made) | Custom units, buildings | Own |
| **Freesound / Sonniss GDC packs** | SFX | CC0 / royalty-free |
| **Kevin MacLeod / Incompetech** | Music | CC-BY |
| **Google Fonts (Noto Sans)** | Fonts — **Noto covers Arabic** | OFL |

**Licence discipline:** every third-party asset is recorded in `docs/ATTRIBUTION.md` with source URL, licence, and date. CC-BY requires attribution in-app (Settings → Credits). Getting this wrong is a legal problem and a store-removal risk, so it is tracked from the first asset, not retrofitted.

**Explicitly avoided:** anything AI-generated where the training-data provenance is unclear, and anything from a "free" pack without an explicit licence file.

---

## 3. Technical specs

### 3D
| Asset | Tris | Textures |
|---|---|---|
| Troop unit | < 800 | 1× 512 albedo, shared atlas per faction |
| Hero | < 2,500 | 1× 1024 |
| Building | < 1,500 | 1× 1024 |
| Environment prop | < 300 | Shared atlas |
| ★ Arena unit (instanced) | < 500 | Shared atlas — instancing requires shared material |

- One material per unit type, **GPU-instancing enabled** (required for 20v20)
- No skinned meshes for common troops — rigid parts + rotation is far cheaper and reads fine at this scale
- Heroes get skeletal animation (they're the ones the player looks at)

### 2D / UI
- Sprites: PNG, power-of-two, packed into atlases per screen group
- Icons: 128×128 source, downscaled by quality tier
- **9-slice** for all panels and buttons
- Compression: ASTC 6×6 (Android), DXT5 (WebGL/desktop)

### Audio
| Type | Format | Settings |
|---|---|---|
| Short SFX | WAV → Vorbis q70 | Decompress on load, mono |
| Long SFX | Vorbis q60 | Compressed in memory |
| Music | Vorbis q50 | **Streaming**, stereo |
| ★ Voice | Handled by LiveKit/Opus, outside the audio pipeline |

Total audio memory budget: **< 30 MB**.

---

## 4. The placeholder-first workflow

```
1. Grey-box with primitives / Kenney assets   → mechanic works?
2. If yes: CC0 asset that roughly fits        → does it read?
3. Only when the mechanic is FINAL: custom art
```

**Never make art for a mechanic that isn't proven.** The single most common way solo projects die is polishing content that gets cut. Every phase gate in `26-ROADMAP.md` is a "is this fun" gate, and art comes after it passes.

Corollary: the game must be **fully playable and shippable with placeholder art**. Art is a quality upgrade, never a dependency.

---

## 5. Audio design

| Category | Notes |
|---|---|
| UI | Tap, confirm, cancel, error, reward, level-up. Short (< 200 ms), consistent family |
| Runner | Shoot, hit, pickup, merge, boss hit, boss death, stage clear |
| Base | Build start/complete, collect, train complete, help received |
| Combat | Per troop type fire, impact, unit death, hero skill (distinct per hero) |
| ★ Arena | Match start, objective captured, kill feed sting, victory/defeat |
| Music | Menu, base (calm loop), world (tense loop), ★ arena (driving loop), victory/defeat |

### Mixing rules
- **Ducking:** ★ voice chat ducks music by 12 dB and SFX by 6 dB while anyone is speaking. Voice must always be intelligible — it's the feature people are paying attention to.
- Separate volume sliders: Master / Music / SFX / **Voice**
- Respect the OS silent switch
- Audio fully off on app pause
- No audio in the first 2 s of cold start (it clashes with OS sounds)

---

## 6. Localisation & fonts

Target languages: **English, Arabic, Spanish, Portuguese, Turkish, Russian, Indonesian, Simplified Chinese** (`02 §4`).

| Concern | Handling |
|---|---|
| Font | **Noto Sans** family — covers Latin, Arabic, Cyrillic, and CJK |
| **RTL** | Arabic requires **full RTL layout mirroring**, not just text direction. Unity's Localization + a mirroring pass on layout groups |
| Text expansion | Design UI for **+40%** over English; German/Russian overflow is the usual failure |
| Numbers | Locale-aware separators; Arabic-Indic digits optional per locale |
| Pipeline | `Localization/*.csv` → Unity StringTables via an editor script |
| Missing keys | Fall back to English and log — never render a raw key |

**Arabic RTL is a real engineering task, not a translation task.** Given the target markets, it is worth doing properly: mirrored layouts, mirrored icons where directional, correct text shaping. Budget time for it in Phase 7, and test with a native reader.

---

## 7. Asset organisation & Addressables

| Group | Load |
|---|---|
| `core` | Bundled in the build — UI, fonts, boot |
| `base` | Bundled |
| `runner` | Bundled |
| `world` | On demand |
| `arena` ★ | On demand |
| `heroes` | On demand, per hero |
| `events` | Remote, so event art ships without an app update |

The `events` remote group matters: it means a live-ops event can introduce new banner art without a store release, matching the server-driven UI design (`14 §7`).

---

## 8. Quality tiers

| Tier | Device | Settings |
|---|---|---|
| Low | < 3 GB RAM | Half-res textures, no post, 30 fps cap, simplified VFX, ★ arena unit cap 20/side visual |
| Medium | 3–6 GB | Full textures, minimal post, 60 fps |
| High | > 6 GB | Everything on |

Auto-detected at boot from RAM + GPU, user-overridable in settings. **Always let the user override** — device detection guesses wrong, and a player who knows their phone can handle more should be allowed to.

---

## Next
- `19-ux-ui-system.md`
- `23-qa-testing.md`

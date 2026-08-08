# 17 — Unity Project Structure

> Unity **6000.5.6f1**, URP, Android + WebGL. Portrait. Mobile-first.

---

## 1. Folder layout

```
client/
├── Assets/
│   ├── _Project/                    ← ALL our content lives here
│   │   ├── Scenes/
│   │   │   ├── Boot.unity           entry: DI, config, auth, then load Main
│   │   │   ├── Main.unity           persistent: UI root, services, camera rig
│   │   │   ├── Runner.unity         additive
│   │   │   ├── Base.unity           additive
│   │   │   ├── World.unity          additive
│   │   │   └── Arena.unity      ★   additive
│   │   ├── Scripts/                 asmdefs, see §2
│   │   ├── Prefabs/{UI,Units,Buildings,VFX,Arena}
│   │   ├── Art/{Sprites,Models,Materials,Fonts,UI}
│   │   ├── Audio/{SFX,Music,UI}
│   │   ├── Data/                    ScriptableObject configs
│   │   ├── Localization/            CSV → StringTables (ar, en, es, pt, tr, ru, id, zh)
│   │   ├── Addressables/            asset groups
│   │   └── Settings/                URP assets, quality tiers
│   ├── Plugins/                     ZeroHour.Sim.dll (built from shared/)
│   └── StreamingAssets/             bootstrap config
├── Packages/manifest.json
└── ProjectSettings/
```

**Rule: everything we author lives under `_Project`.** Third-party packages stay in `Packages`/`Plugins`. This makes "what did we write" answerable at a glance and keeps asset store imports from scattering.

---

## 2. Assembly definitions

Assemblies exist to keep compile times low and dependencies honest.

```
ZeroHour.Core        → ZeroHour.Sim (dll), no other project refs
ZeroHour.Net         → Core
ZeroHour.UI          → Core
ZeroHour.Runner      → Core, UI
ZeroHour.Base        → Core, UI
ZeroHour.World       → Core, UI, Net
ZeroHour.Arena  ★    → Core, UI, Net, Voice
ZeroHour.Voice  ★    → Core
ZeroHour.Bridge      → Editor-only (see 28)
ZeroHour.Tests       → all, Editor+test platforms only
```

**Dependency rule: gameplay assemblies never reference each other.** `Runner` must not know `World` exists. They communicate through `Core` services and an event bus. This is what allows Phase 1 to ship before Phase 3 is designed.

---

## 3. Service architecture

```csharp
// Boot.unity: single composition root
public sealed class Bootstrap : MonoBehaviour {
    async void Start() {
        var c = new ServiceContainer();
        c.Register<IClock>(new ServerSyncedClock());
        c.Register<IConfigService>(new ConfigService());
        c.Register<ISaveService>(new SaveService());
        c.Register<IApiClient>(new ApiClient());
        c.Register<IEventBus>(new EventBus());
        c.Register<IAudioService>(new AudioService());
        c.Register<IVoiceService>(new MockVoiceService());   // ★ swapped in Phase 4
        ServiceLocator.Set(c);

        await c.Get<IConfigService>().LoadAsync();
        await SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
    }
}
```

Constructor injection inside plain C# classes; a service locator only at MonoBehaviour boundaries. No third-party DI framework — the dependency graph is small enough that one isn't worth the build-time cost.

### Core services
| Service | Responsibility |
|---|---|
| `IClock` | **Server-synced time.** Never `DateTime.Now` in gameplay |
| `IConfigService` | Balance tables, hot reload on `configVersion` mismatch |
| `ISaveService` | Local cache of last known state (display only, never authoritative) |
| `IApiClient` | REST with retry, backoff, offline queue |
| `IRealtimeClient` | SignalR hubs |
| `IEventBus` | Decoupled cross-assembly messaging |
| `IAudioService` | Pooled SFX, music with ducking |
| `IVoiceService` ★ | Voice abstraction (`10 §4.1`) |
| `IAnalytics` | Batched event queue |

---

## 4. Scene strategy

`Boot` → `Main` (persistent) → gameplay scenes loaded additively and unloaded on exit.

- `Main` owns: UI root canvas, service MonoBehaviours, audio listener, the persistent ★ voice widget
- Only one gameplay scene is loaded at a time
- Transitions go through a loading veil that hides the additive load/unload

**Why additive over single-scene loads:** the ★ voice widget and UI root must survive scene changes. A player on a voice call who taps from Base to World cannot be disconnected by a scene load.

---

## 5. Performance budgets (mobile)

| Metric | Budget |
|---|---|
| Target FPS | 60 (30 floor on low-end) |
| Draw calls | < 100 base/world, < 150 ★ arena |
| Triangles | < 150k on screen |
| Texture memory | < 200 MB |
| Total RAM | < 700 MB |
| APK (AAB) size | < 150 MB |
| Cold start → playable | < 5 s |
| GC allocation in steady state | **0 B/frame** |

### Techniques
- **Object pooling** for units, projectiles, VFX, damage numbers, UI list items
- **GPU instancing** for repeated units — mandatory for ★ 20v20 arena
- Sprite atlases per screen group
- Addressables for on-demand content
- 3 quality tiers auto-selected by device, user-overridable
- **No allocation in `Update`.** Cache, pool, reuse. Verified with the profiler each phase.
- Runner and arena run their sim step at a fixed rate, decoupled from render

---

## 6. Packages

**Required:** Universal RP, Input System, Addressables, Localization, TextMeshPro, Unity Test Framework, Newtonsoft Json, Mobile Notifications, Google Play Billing (Phase 8), `com.unity.webrtc` ★ (Phase 4)

**Explicitly not used:** Unity Netcode for GameObjects (we have our own arena netcode), Cinemachine (our cameras are simple), any paid asset until revenue exists.

---

## 7. Build settings

| Setting | Value |
|---|---|
| Scripting backend | IL2CPP |
| Target architecture | ARM64 only |
| Managed stripping | Medium (High breaks reflection in serialization) |
| Compression | LZ4HC |
| Graphics API | Vulkan, OpenGLES3 fallback |
| Min Android | API 24 (7.0) |
| Target Android | API 35 |
| Orientation | Portrait only |
| Package format | AAB (Play requirement) |

**WebGL** is kept working as a dev convenience: it gives a shareable playable link with no install, which makes early playtesting far easier. It is not a shipping target.

---

## 8. Coding conventions

```csharp
// Interfaces  IThing        Private fields  _camelCase
// Async       ThingAsync    Constants       PascalCase
// Namespaces mirror assembly names: ZeroHour.Arena.Views

// Every gameplay number comes from config, never a literal:
var time = _config.Buildings.BuildTimeMs(defId, level);   // ✅
var time = 20000 * Mathf.Pow(1.42f, level);                // ❌
```

- `#nullable enable` in all our assemblies
- No `public` fields except in serialized data classes
- `[SerializeField] private` over `public` for inspector wiring
- Views never contain game logic. Logic lives in `Sim` or a service.

---

## 9. Testing in Unity

| Type | Location |
|---|---|
| Sim tests | `server/ZeroHour.Tests` (plain .NET, fast, no Unity) |
| Edit-mode tests | `Assets/_Project/Scripts/Tests/EditMode` |
| Play-mode tests | `.../PlayMode` — scene loading, UI flows |
| Determinism | Same fixtures run in .NET and Unity; hashes must match |

**The sim is tested outside Unity.** It has no Unity dependency, so its tests run in under a second in CI without an editor licence. That speed is what makes TDD on the sim practical.

---

## Next
- `18-art-audio-pipeline.md`
- `19-ux-ui-system.md`
- `27-unity-editor-guide.md`

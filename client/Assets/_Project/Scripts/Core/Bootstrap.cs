using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZeroHour.Core
{
    /// <summary>
    /// Single composition root, living in Boot.unity (docs/17 §3).
    ///
    /// Every service is constructed here and nowhere else. When startup order matters — and
    /// it does, since config load depends on the clock — having one readable sequence beats
    /// discovering the order empirically across a dozen Awake calls.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField]
        private string nextScene = "Main";

        private async void Start()
        {
            // Boot must survive the additive load of Main.
            DontDestroyOnLoad(gameObject);

            try
            {
                await InitialiseAsync();
            }
            catch (Exception ex)
            {
                // A failure here means the game cannot run at all. Surface it loudly rather
                // than loading Main into a half-built container, which fails later with a
                // stack trace pointing at the wrong place.
                Debug.LogError($"[Bootstrap] Startup failed: {ex}");
            }
        }

        private async Task InitialiseAsync()
        {
            float startedAt = Time.realtimeSinceStartup;

            var container = new ServiceContainer();

            // Registration order mirrors the dependency order: the clock underpins config
            // expiry and every timer, so it comes first.
            var clock = new ServerSyncedClock();
            container.Register<IClock>(clock);
            container.Register<IEventBus>(new EventBus());
            container.Register<ISaveService>(new SaveService());

            var config = new ConfigService();
            container.Register<IConfigService>(config);

            ServiceLocator.Set(container);

            await config.LoadAsync();

            Debug.Log($"[Bootstrap] Services ready in {(Time.realtimeSinceStartup - startedAt) * 1000f:F0} ms " +
                      $"(config v{config.Version}).");

            // Additive: Boot stays resident so its services outlive any scene swap.
            if (!string.IsNullOrEmpty(nextScene))
            {
                await LoadAdditiveAsync(nextScene);
            }
        }

        private static Task LoadAdditiveAsync(string sceneName)
        {
            var completion = new TaskCompletionSource<bool>();

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                // Almost always a scene missing from Build Settings, which is otherwise a
                // silent no-op that looks like a hang.
                Debug.LogError($"[Bootstrap] Could not load '{sceneName}'. Is it in Build Settings?");
                completion.SetResult(false);
                return completion.Task;
            }

            operation.completed += _ =>
            {
                Debug.Log($"[Bootstrap] Loaded '{sceneName}'.");
                completion.SetResult(true);
            };

            return completion.Task;
        }

        private void OnDestroy()
        {
            // Play-mode runs reuse the domain when Enter Play Mode Options are on, so a stale
            // container would otherwise leak into the next run.
            ServiceLocator.Reset();
        }
    }
}

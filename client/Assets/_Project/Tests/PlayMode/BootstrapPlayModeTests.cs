using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZeroHour.Core;

namespace ZeroHour.Tests.PlayMode
{
    /// <summary>
    /// Play-mode coverage for the composition root (docs/17 §3).
    ///
    /// These deliberately assert things an edit-mode test cannot reach. Bootstrap only runs
    /// from `Start`, its initialisation is `async`, and `DontDestroyOnLoad` plus additive scene
    /// loading only mean anything with a live player loop — so an edit-mode test of the same
    /// code would prove that the classes construct, not that startup works.
    ///
    /// Until now Bootstrap was only ever verified by a human pressing Play and reading the
    /// console, which is exactly the kind of check that quietly stops happening.
    /// </summary>
    public class BootstrapPlayModeTests
    {
        [SetUp]
        public void ResetLocator()
        {
            // Enter Play Mode Options are on for this project, so the domain is reused between
            // runs and a container from a previous test would otherwise still be registered.
            ServiceLocator.Reset();
        }

        [TearDown]
        public void ClearLocator()
        {
            ServiceLocator.Reset();
        }

        /// <summary>
        /// Boot.unity must bring every service up and load Main additively.
        ///
        /// This is the startup path the whole game depends on, and it is asserted end to end
        /// rather than by re-registering services by hand: a test that builds its own container
        /// would pass even if Bootstrap itself were broken.
        /// </summary>
        [UnityTest]
        public IEnumerator Boot_BringsUpServices_AndLoadsMainAdditively()
        {
            yield return LoadBootScene();

            // Wait on the *last* thing startup does, not the first. Bootstrap calls
            // ServiceLocator.Set before awaiting config and before loading Main, so waiting on
            // IsReady returns while startup is still in flight — this test failed on exactly
            // that, asserting Main was loaded when only registration had happened.
            yield return WaitUntilSceneLoaded("Main");

            Assert.IsTrue(ServiceLocator.IsReady, "Bootstrap did not publish a service container.");

            Assert.IsNotNull(ServiceLocator.Get<IClock>(), "IClock was not registered.");
            Assert.IsNotNull(ServiceLocator.Get<IEventBus>(), "IEventBus was not registered.");
            Assert.IsNotNull(ServiceLocator.Get<ISaveService>(), "ISaveService was not registered.");

            var config = ServiceLocator.Get<IConfigService>();
            Assert.IsNotNull(config, "IConfigService was not registered.");
            Assert.IsTrue(config.IsLoaded, "Bootstrap continued before config finished loading.");

            // Additive, not single: Boot must survive so its services outlive the scene swap.
            Assert.IsTrue(SceneManager.GetSceneByName("Main").isLoaded,
                "Main was not loaded additively by Bootstrap.");
            Assert.IsTrue(SceneManager.GetSceneByName("Boot").isLoaded,
                "Boot was unloaded; services would not survive the scene swap.");
        }

        /// <summary>
        /// The clock must measure durations against monotonic uptime, not wall-clock time.
        ///
        /// Only meaningful in play mode, since <c>Time.realtimeSinceStartupAsDouble</c> does not
        /// advance without a running player loop. The property matters because every timer in
        /// the economy hangs off it, and a device clock is user-settable (docs/17 §3).
        /// </summary>
        [UnityTest]
        public IEnumerator Clock_UptimeAdvances_AcrossFrames()
        {
            var clock = new ServerSyncedClock();

            double before = clock.Uptime;
            yield return null;
            yield return null;

            Assert.Greater(clock.Uptime, before, "Uptime did not advance across frames.");
        }

        /// <summary>
        /// A subscriber that throws must not stop the remaining subscribers from being called.
        ///
        /// Worth asserting in play mode specifically: EventBus reports the failure through
        /// <c>Debug.LogError</c>, and an unhandled error log fails a play-mode test by default,
        /// so this also pins the contract that the bus swallows-and-reports rather than
        /// propagating.
        /// </summary>
        [UnityTest]
        public IEnumerator EventBus_OneThrowingHandler_DoesNotStopTheOthers()
        {
            var bus = new EventBus();
            bool secondRan = false;

            bus.Subscribe<string>(_ => throw new System.InvalidOperationException("deliberate"));
            bus.Subscribe<string>(_ => secondRan = true);

            // The bus logs the failure; without this the expected error fails the test.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("deliberate"));

            bus.Publish("ping");
            yield return null;

            Assert.IsTrue(secondRan, "A throwing handler prevented later handlers from running.");
        }

        private static IEnumerator LoadBootScene()
        {
            // Boot is build index 0; loading by name keeps the test readable if that changes.
            AsyncOperation load = SceneManager.LoadSceneAsync("Boot", LoadSceneMode.Single);
            Assert.IsNotNull(load, "Boot.unity is missing from Build Settings.");

            while (!load.isDone)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitUntilSceneLoaded(string sceneName)
        {
            // Bounded rather than open-ended: a hung startup should fail with a clear message
            // instead of stalling the run until the outer timeout kills it.
            const float TimeoutSeconds = 10f;
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;

            while (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("Bootstrap did not load '" + sceneName + "' within "
                        + TimeoutSeconds + "s.");
                }

                yield return null;
            }
        }
    }
}

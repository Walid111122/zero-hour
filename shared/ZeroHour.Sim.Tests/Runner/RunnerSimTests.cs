using System;
using System.Collections.Generic;
using Xunit;
using ZeroHour.Sim;
using ZeroHour.Sim.Runner;

namespace ZeroHour.Sim.Tests.Runner
{
    /// <summary>
    /// Movement and tick-contract tests for <see cref="RunnerSim"/> (docs/06 §3, phase-1 §1.1).
    /// </summary>
    public class RunnerSimTests
    {
        private static RunnerState Fresh => RunnerState.Start(Squad.Start(10, TroopType.Tank));

        [Fact]
        public void Tick_Advances_By_One_Per_Step()
        {
            RunnerState state = Fresh;

            for (int i = 1; i <= 10; i++)
            {
                state = RunnerSim.Step(state, RunnerInput.Center);
                Assert.Equal(i, state.Tick);
            }
        }

        [Fact]
        public void Elapsed_Time_Comes_From_The_Tick_Count()
        {
            RunnerState state = Fresh;

            for (int i = 0; i < RunnerTuning.TicksPerSecond; i++)
            {
                state = RunnerSim.Step(state, RunnerInput.Center);
            }

            Assert.Equal(1000, state.ElapsedMs);
        }

        [Fact]
        public void Squad_Advances_At_RunSpeed()
        {
            // One second of ticks must cover exactly RunSpeed units. This is the property that
            // ties stage lengthUnits to the 55 s median duration target in docs/06 §6.
            RunnerState state = Fresh;

            for (int i = 0; i < RunnerTuning.TicksPerSecond; i++)
            {
                state = RunnerSim.Step(state, RunnerInput.Center);
            }

            Assert.Equal(RunnerTuning.RunSpeed, state.Distance);
        }

        [Fact]
        public void Distance_Accumulates_Without_Drift_Over_A_Long_Stage()
        {
            // 75 s is the stage ceiling from docs/06 §6. Fixed-point addition must not drift
            // across 1,500 ticks; if it did, client and server would disagree on whether the
            // player reached the boss.
            RunnerState state = Fresh;
            int ticks = 75 * RunnerTuning.TicksPerSecond;

            for (int i = 0; i < ticks; i++)
            {
                state = RunnerSim.Step(state, RunnerInput.Center);
            }

            Assert.Equal(RunnerTuning.RunSpeed * 75, state.Distance);
        }

        [Fact]
        public void Steering_Moves_Toward_The_Target_And_Stops_There()
        {
            RunnerState state = Fresh;
            RunnerInput right = new RunnerInput(RunnerTuning.MaxX);

            // Crossing 3 units at 0.6 u/tick looks like 5 ticks of arithmetic, but takes 6.
            // 0.6 has no exact 32.32 representation, so each step falls a few raw units short
            // and five of them land just under the edge; the sixth snaps on. Asserting 6 here
            // rather than loosening the comparison keeps the rounding visible instead of
            // hiding it behind a tolerance.
            for (int i = 0; i < 5; i++)
            {
                state = RunnerSim.Step(state, right);
            }

            Assert.NotEqual(RunnerTuning.MaxX, state.X);
            Assert.True(state.X < RunnerTuning.MaxX);

            state = RunnerSim.Step(state, right);
            Assert.Equal(RunnerTuning.MaxX, state.X);

            // Holding the same input must not push past the edge.
            state = RunnerSim.Step(state, right);
            Assert.Equal(RunnerTuning.MaxX, state.X);
        }

        [Fact]
        public void Steering_Never_Overshoots_A_Nearby_Target()
        {
            // A target closer than one tick of travel snaps exactly, with no residual. A
            // percentage lerp would leave a tiny remainder here and pollute the fingerprint.
            RunnerState state = Fresh;
            Fixed nudge = Fixed.FromFraction(1, 100);

            state = RunnerSim.Step(state, new RunnerInput(nudge));

            Assert.Equal(nudge, state.X);
        }

        [Fact]
        public void Steering_Is_Symmetric_Left_And_Right()
        {
            RunnerState left = Fresh;
            RunnerState right = Fresh;

            for (int i = 0; i < 3; i++)
            {
                left = RunnerSim.Step(left, new RunnerInput(RunnerTuning.MinX));
                right = RunnerSim.Step(right, new RunnerInput(RunnerTuning.MaxX));
            }

            Assert.Equal(left.X, -right.X);
        }

        [Fact]
        public void Squad_Cannot_Leave_The_Corridor()
        {
            RunnerState state = Fresh;
            RunnerInput farRight = new RunnerInput(Fixed.FromInt(1000));

            for (int i = 0; i < 60; i++)
            {
                state = RunnerSim.Step(state, farRight);
            }

            Assert.Equal(RunnerTuning.MaxX, state.X);
            Assert.True(state.X <= RunnerTuning.MaxX);
        }

        [Fact]
        public void Input_Is_Clamped_To_The_Corridor_On_Construction()
        {
            Assert.Equal(RunnerTuning.MaxX, new RunnerInput(Fixed.FromInt(99)).TargetX);
            Assert.Equal(RunnerTuning.MinX, new RunnerInput(Fixed.FromInt(-99)).TargetX);
        }

        [Fact]
        public void Lane_Centers_Are_Evenly_Spaced_Inside_The_Corridor()
        {
            // Three lanes across six units: centres at -2, 0, +2.
            Assert.Equal(Fixed.FromInt(-2), RunnerTuning.LaneCenterX(0));
            Assert.Equal(Fixed.Zero, RunnerTuning.LaneCenterX(1));
            Assert.Equal(Fixed.FromInt(2), RunnerTuning.LaneCenterX(2));

            for (int lane = 0; lane < RunnerTuning.LaneCount; lane++)
            {
                Fixed x = RunnerTuning.LaneCenterX(lane);
                Assert.True(x >= RunnerTuning.MinX && x <= RunnerTuning.MaxX);
            }
        }

        [Fact]
        public void Shield_Expires_On_Schedule_And_Never_Goes_Negative()
        {
            Squad shielded = Gate.Shield(100).Apply(Squad.Start(10, TroopType.Tank));
            RunnerState state = new RunnerState(0, shielded, Fixed.Zero, Fixed.Zero);

            state = RunnerSim.Step(state, RunnerInput.Center);
            Assert.Equal(50, state.Squad.ShieldMs);

            state = RunnerSim.Step(state, RunnerInput.Center);
            Assert.Equal(0, state.Squad.ShieldMs);
            Assert.False(state.Squad.IsShielded);

            // Stepping past expiry must not drive the shield negative.
            state = RunnerSim.Step(state, RunnerInput.Center);
            Assert.Equal(0, state.Squad.ShieldMs);
        }

        [Fact]
        public void Step_Does_Not_Mutate_The_Input_State()
        {
            RunnerState original = Fresh;
            RunnerSim.Step(original, new RunnerInput(RunnerTuning.MaxX));

            Assert.Equal(0, original.Tick);
            Assert.Equal(Fixed.Zero, original.X);
            Assert.Equal(Fixed.Zero, original.Distance);
        }

        [Fact]
        public void Same_Input_Log_Produces_The_Same_Final_Fingerprint_Over_100_Runs()
        {
            // phase-1 §1.1: "Same seed + same inputs => identical final state hash, 100 runs."
            // This covers the movement half; the combat half joins it when enemies land.
            RunnerInput[] log = BuildInputLog(600);
            ulong first = Replay(log);

            for (int run = 0; run < 100; run++)
            {
                Assert.Equal(first, Replay(log));
            }
        }

        [Fact]
        public void A_Different_Input_Log_Produces_A_Different_Fingerprint()
        {
            // Guards against a fingerprint that ignores the fields it is meant to cover: a
            // hash that never changes would make the test above pass for the wrong reason.
            // The change has to reach the end of the run, hence the altered tail — see the
            // test below for why a single changed tick would not do.
            RunnerInput[] log = BuildInputLog(600);
            RunnerInput[] altered = (RunnerInput[])log.Clone();

            for (int i = 500; i < altered.Length; i++)
            {
                altered[i] = new RunnerInput(RunnerTuning.MinX);
            }

            Assert.NotEqual(Replay(log), Replay(altered));
        }

        [Fact]
        public void A_Single_Changed_Tick_Leaves_No_Trace_In_The_Final_State()
        {
            // Worth pinning because it shapes what server-side validation can detect. Steering
            // converges exactly onto the target and distance is derived from the tick count,
            // so a one-tick input blip that the squad steers back from is invisible in the
            // final state. The server compares outcomes, not keystrokes: two players who end
            // a stage identically are indistinguishable, and legitimately so.
            RunnerInput[] log = BuildInputLog(600);
            RunnerInput[] blipped = (RunnerInput[])log.Clone();
            blipped[300] = new RunnerInput(RunnerTuning.MinX);

            Assert.Equal(Replay(log), Replay(blipped));
        }

        [Fact]
        public void Replaying_From_A_Mid_Stage_Snapshot_Rejoins_The_Original_Timeline()
        {
            // The server does not always replay from tick zero; being able to resume from a
            // snapshot is what makes RunnerState the save-and-replay unit it claims to be.
            RunnerInput[] log = BuildInputLog(400);
            RunnerState state = Fresh;
            var snapshots = new List<RunnerState>();

            foreach (RunnerInput input in log)
            {
                snapshots.Add(state);
                state = RunnerSim.Step(state, input);
            }

            RunnerState resumed = snapshots[200];

            for (int i = 200; i < log.Length; i++)
            {
                resumed = RunnerSim.Step(resumed, log[i]);
            }

            Assert.Equal(state.Fingerprint(), resumed.Fingerprint());
        }

        /// <summary>
        /// Builds a deterministic, varied input log. Uses <see cref="DetRandom"/> with a fixed
        /// seed rather than <c>System.Random</c> so a failure is always reproducible.
        /// </summary>
        private static RunnerInput[] BuildInputLog(int ticks)
        {
            var random = DetRandom.FromState(0x5EED_1234UL, 0xC0FFEE_99UL);
            var log = new RunnerInput[ticks];

            for (int i = 0; i < ticks; i++)
            {
                // Hold a lane for a stretch, then pick a new one, which is roughly how a real
                // player drags rather than jittering every tick.
                if (i % 7 == 0)
                {
                    log[i] = RunnerInput.ToLane(random.Range(0, RunnerTuning.LaneCount));
                }
                else
                {
                    log[i] = i == 0 ? RunnerInput.Center : log[i - 1];
                }
            }

            return log;
        }

        private static ulong Replay(RunnerInput[] log)
        {
            RunnerState state = Fresh;

            foreach (RunnerInput input in log)
            {
                state = RunnerSim.Step(state, input);
            }

            return state.Fingerprint();
        }
    }
}

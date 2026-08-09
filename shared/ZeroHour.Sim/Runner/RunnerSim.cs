namespace ZeroHour.Sim.Runner
{
    /// <summary>
    /// The deterministic runner simulation (docs/06 §11.2).
    /// <para>
    /// Pure static logic with no Unity types, no wall clock and no ambient randomness. The
    /// client plays a stage and uploads its input log; the server replays that log through
    /// this same compiled method and rejects the reward if the result differs (docs/06 §11.3).
    /// That is the entire anti-cheat design for the runner, and it only holds while this class
    /// stays a pure function of its arguments.
    /// </para>
    /// </summary>
    public static class RunnerSim
    {
        /// <summary>
        /// Advances the simulation by exactly one fixed tick.
        /// <para>
        /// <b>There is no <c>dt</c> parameter</b>, though phase-1 §1.1 sketches the signature
        /// as <c>Step(state, input, dt)</c>. Accepting a caller-supplied delta is the standard
        /// way a deterministic sim quietly stops being deterministic: a client stepping at
        /// frame time and a server stepping at a fixed rate would accumulate different
        /// rounding, and honest players would fail reward validation. The tick length is
        /// <see cref="RunnerTuning.TickDelta"/>, and the view layer interpolates between
        /// states for rendering rather than driving the sim at frame rate (docs/17 §5).
        /// </para>
        /// </summary>
        /// <param name="state">The state at the start of the tick.</param>
        /// <param name="input">The player's intent for this tick.</param>
        /// <returns>The state after one tick.</returns>
        public static RunnerState Step(in RunnerState state, in RunnerInput input)
        {
            int tick = state.Tick + 1;
            Fixed x = StepSteering(state.X, input.TargetX);

            // Derived from the tick count, never accumulated. Adding a per-tick step drifts,
            // because 12/20 has no exact 32.32 representation; over a 1,500-tick stage that
            // error is enough to disagree with the server about whether the boss was reached.
            Fixed distance = RunnerTuning.DistanceAtTick(tick);
            Squad squad = StepShield(state.Squad);

            return new RunnerState(tick, squad, x, distance);
        }

        /// <summary>
        /// Moves the squad toward the target at a capped speed, never overshooting.
        /// <para>
        /// Capped speed rather than a percentage lerp: a lerp of the form
        /// <c>x += (target - x) * k</c> is frame-rate dependent by construction and never
        /// quite arrives, leaving a residual that pollutes the state fingerprint. Moving at a
        /// fixed rate and snapping on arrival is exactly reproducible and gives the squad a
        /// consistent, learnable feel.
        /// </para>
        /// </summary>
        /// <param name="x">Current squad centre X.</param>
        /// <param name="targetX">Desired squad centre X.</param>
        /// <returns>The new X.</returns>
        private static Fixed StepSteering(Fixed x, Fixed targetX)
        {
            Fixed delta = targetX - x;
            Fixed maxStep = RunnerTuning.SteerStepPerTick;

            if (Fixed.Abs(delta) <= maxStep)
            {
                return targetX;
            }

            return delta > Fixed.Zero ? x + maxStep : x - maxStep;
        }

        /// <summary>Expires the shield by one tick, floored at zero.</summary>
        /// <param name="squad">The squad at the start of the tick.</param>
        /// <returns>The squad with its shield decremented.</returns>
        private static Squad StepShield(Squad squad)
        {
            if (!squad.IsShielded)
            {
                return squad;
            }

            int remaining = squad.ShieldMs - RunnerTuning.MsPerTick;

            return squad.WithShieldMs(remaining < 0 ? 0 : remaining);
        }
    }
}

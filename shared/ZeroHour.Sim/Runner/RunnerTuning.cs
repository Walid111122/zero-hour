namespace ZeroHour.Sim.Runner
{
    /// <summary>
    /// Fixed constants for the runner sim: tick rate, corridor geometry, movement speeds.
    /// <para>
    /// <b>Most of these are placeholders</b> destined for <c>tools/balance/runner_*.csv</c>
    /// (docs/06 §12). They live here so the sim can run before the data pipeline exists; the
    /// exception is <see cref="TicksPerSecond"/>, which is a genuine architectural decision
    /// rather than a balance number and should not move into data.
    /// </para>
    /// </summary>
    public static class RunnerTuning
    {
        /// <summary>
        /// Simulation rate, in ticks per second.
        /// <para>
        /// <b>This is a decision the design docs do not make.</b> docs/16 and docs/11 fix the
        /// <i>arena</i> at 20 Hz, and docs/17 only says the runner steps "at a fixed rate,
        /// decoupled from render". Three things argue for matching 20 Hz here:
        /// </para>
        /// <para>
        /// 1. The server re-simulates every stage clear to validate the reward (docs/06 §11.3),
        /// so tick count is a direct server CPU cost multiplied by the whole player base.
        /// </para>
        /// <para>
        /// 2. phase-1 §1.1 budgets a full 20-stage playthrough at under 50 ms. Twenty stages of
        /// ~55 s is about 1,100 s of play — 22,000 ticks at 20 Hz, leaving ~2.3 µs per tick.
        /// At 60 Hz the same budget allows 0.76 µs per tick, which is not realistic once
        /// enemies and collisions land.
        /// </para>
        /// <para>
        /// 3. The client uploads its whole input log for re-validation. 20 Hz keeps a 75 s
        /// stage at 1,500 entries rather than 4,500.
        /// </para>
        /// <para>
        /// Steering feel does not suffer, because the view layer interpolates between sim
        /// states at render rate. Gate crossings stay exact at any tick rate because they are
        /// resolved by interval crossing rather than by proximity.
        /// </para>
        /// </summary>
        public const int TicksPerSecond = 20;

        /// <summary>Milliseconds per tick (50). Integer, so tick/ms conversion stays exact.</summary>
        public const int MsPerTick = 1000 / TicksPerSecond;

        /// <summary>
        /// Duration of one tick in seconds, as fixed-point.
        /// <para>
        /// The sim deliberately has no variable <c>dt</c>. A timestep that varies with frame
        /// rate is the classic way determinism dies: the client and the re-simulating server
        /// would step differently and the reward check would reject honest players.
        /// </para>
        /// <para>
        /// <b>Do not build per-tick movement by multiplying a speed by this value.</b>
        /// <see cref="Fixed"/> is binary 32.32, so 1/20 has no exact representation and the
        /// product carries a rounding error that compounds every tick. Divide the speed by
        /// <see cref="TicksPerSecond"/> instead, which rounds once, and prefer
        /// <see cref="DistanceAtTick"/> where a quantity can be derived rather than
        /// accumulated. This constant remains for converting durations for display.
        /// </para>
        /// </summary>
        public static Fixed TickDelta => Fixed.FromFraction(1, TicksPerSecond);

        /// <summary>Corridor width in world units (docs/06 §3).</summary>
        public static Fixed CorridorWidth => Fixed.FromInt(6);

        /// <summary>Number of lanes across the corridor (docs/06 §3).</summary>
        public const int LaneCount = 3;

        /// <summary>Rightmost X the squad centre may reach.</summary>
        public static Fixed MaxX => CorridorWidth / 2;

        /// <summary>Leftmost X the squad centre may reach.</summary>
        public static Fixed MinX => -MaxX;

        /// <summary>
        /// Forward speed in world units per second. Placeholder: paired with the
        /// <c>lengthUnits</c> column of <c>runner_stages.csv</c>, this sets stage duration,
        /// and docs/06 §6 targets a 55 s median. 12 u/s puts a 55 s stage at 660 units.
        /// </summary>
        public static Fixed RunSpeed => Fixed.FromInt(12);

        /// <summary>
        /// Lateral steering speed in world units per second. Placeholder tuned for feel:
        /// crossing the full 6-unit corridor takes about half a second, which is responsive
        /// without making the squad feel weightless.
        /// </summary>
        public static Fixed SteerSpeed => Fixed.FromInt(12);

        /// <summary>
        /// Lateral distance the squad may cover in one tick.
        /// <para>
        /// Derived by dividing the speed once rather than multiplying by
        /// <see cref="TickDelta"/>, which keeps the rounding to a single step. Steering can
        /// safely accumulate this value because <c>RunnerSim</c> snaps to the target once it
        /// is within one step, so X converges exactly onto the target instead of drifting.
        /// </para>
        /// </summary>
        public static Fixed SteerStepPerTick => SteerSpeed / TicksPerSecond;

        /// <summary>
        /// Distance travelled after a given number of ticks.
        /// <para>
        /// Derived from the tick count rather than accumulated per tick. Accumulating a
        /// per-tick step drifts, because a 32.32 binary fixed-point cannot hold 12/20 exactly
        /// and the error compounds; multiplying by the integer tick count first is exact, so
        /// the single trailing division is the only rounding in the whole stage. At 20 Hz and
        /// 12 u/s this lands on whole units at every second.
        /// </para>
        /// <para>
        /// This holds while forward speed is constant, which it is today. When speed-changing
        /// pickups arrive, the same trick generalises: keep the distance banked at the start of
        /// each constant-speed segment and derive within the segment, so exactness is
        /// preserved per segment rather than lost across the stage.
        /// </para>
        /// </summary>
        /// <param name="tick">Ticks elapsed since the stage began.</param>
        /// <returns>Distance travelled, in world units.</returns>
        public static Fixed DistanceAtTick(int tick) => (RunSpeed * tick) / TicksPerSecond;

        /// <summary>Returns the centre X of a lane.</summary>
        /// <param name="lane">Zero-based lane index, left to right.</param>
        /// <returns>The lane centre, clamped into the corridor.</returns>
        public static Fixed LaneCenterX(int lane)
        {
            // Lane width is corridorWidth / laneCount; centre of lane i sits half a lane in
            // from its left edge.
            Fixed laneWidth = CorridorWidth / LaneCount;
            Fixed center = MinX + (laneWidth * lane) + (laneWidth / 2);

            return Fixed.Clamp(center, MinX, MaxX);
        }
    }
}

using System;

namespace ZeroHour.Sim.Runner
{
    /// <summary>
    /// The complete state of a runner stage at one tick.
    /// <para>
    /// This struct <i>is</i> the save-and-replay unit: given a stage, a seed and an input log,
    /// replaying every tick must land on a bit-identical state, because the server re-runs the
    /// same code to validate the reward (docs/06 §11.3).
    /// </para>
    /// <para>
    /// Enemy and boss fields are deliberately absent so far — they arrive with the combat
    /// slice. What is here is kinematics and the squad, which is everything the movement step
    /// needs and nothing it does not.
    /// </para>
    /// </summary>
    public readonly struct RunnerState : IEquatable<RunnerState>
    {
        /// <summary>Ticks elapsed since the stage began. Multiply by MsPerTick for wall time.</summary>
        public readonly int Tick;

        /// <summary>The player's squad.</summary>
        public readonly Squad Squad;

        /// <summary>Squad centre X within the corridor.</summary>
        public readonly Fixed X;

        /// <summary>Distance travelled down the corridor, in world units.</summary>
        public readonly Fixed Distance;

        /// <summary>Creates a state.</summary>
        /// <param name="tick">Ticks elapsed.</param>
        /// <param name="squad">The squad.</param>
        /// <param name="x">Squad centre X; clamped to the corridor.</param>
        /// <param name="distance">Distance travelled.</param>
        public RunnerState(int tick, Squad squad, Fixed x, Fixed distance)
        {
            Tick = tick;
            Squad = squad;
            X = Fixed.Clamp(x, RunnerTuning.MinX, RunnerTuning.MaxX);
            Distance = distance;
        }

        /// <summary>Creates the opening state of a stage: centred, at distance zero.</summary>
        /// <param name="squad">The starting squad.</param>
        /// <returns>The initial state.</returns>
        public static RunnerState Start(Squad squad) =>
            new RunnerState(0, squad, Fixed.Zero, Fixed.Zero);

        /// <summary>Elapsed stage time in milliseconds, derived from the tick count.</summary>
        public int ElapsedMs => Tick * RunnerTuning.MsPerTick;

        /// <summary>Returns a copy carrying a different squad.</summary>
        /// <param name="squad">The new squad.</param>
        /// <returns>The updated state.</returns>
        public RunnerState WithSquad(Squad squad) => new RunnerState(Tick, squad, X, Distance);

        /// <summary>
        /// Folds the state into a determinism fingerprint.
        /// <para>
        /// This is what the determinism tests compare and what a desync report would carry, so
        /// every field that affects gameplay has to be folded in here. A field added to the
        /// struct but forgotten here would make two genuinely different states look identical.
        /// </para>
        /// </summary>
        /// <param name="hash">The accumulator to fold into.</param>
        /// <returns>The updated accumulator.</returns>
        public Hash Fold(Hash hash) => Squad.Fold(hash.Add(Tick)).Add(X).Add(Distance);

        /// <summary>The state fingerprint, as compared by determinism tests.</summary>
        /// <returns>The hash value.</returns>
        public ulong Fingerprint() => Fold(Hash.Create()).Value;

        /// <inheritdoc />
        public bool Equals(RunnerState other) =>
            Tick == other.Tick &&
            Squad.Equals(other.Squad) &&
            X == other.X &&
            Distance == other.Distance;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is RunnerState other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (int)Fingerprint();

        /// <inheritdoc />
        public override string ToString() =>
            "t" + Tick.ToString() + " " + Squad.ToString() +
            " x=" + X.ToString() + " d=" + Distance.ToString();
    }
}

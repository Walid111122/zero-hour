using System;

namespace ZeroHour.Sim.Runner
{
    /// <summary>
    /// One tick of player input: where the finger wants the squad to be (docs/06 §1).
    /// <para>
    /// The whole game is one finger dragging horizontally, so a tick of input is a single
    /// target X. Storing the <i>target</i> rather than a delta is what makes the input log
    /// replayable: a dropped or duplicated tick cannot drift the squad off course, because
    /// each entry states an absolute intent rather than a relative nudge.
    /// </para>
    /// </summary>
    public readonly struct RunnerInput : IEquatable<RunnerInput>
    {
        /// <summary>Desired squad centre X, already clamped to the corridor.</summary>
        public readonly Fixed TargetX;

        /// <summary>Creates an input, clamping the target into the corridor.</summary>
        /// <param name="targetX">Desired squad centre X.</param>
        public RunnerInput(Fixed targetX)
        {
            TargetX = Fixed.Clamp(targetX, RunnerTuning.MinX, RunnerTuning.MaxX);
        }

        /// <summary>Input holding the squad in the centre of the corridor.</summary>
        public static RunnerInput Center => new RunnerInput(Fixed.Zero);

        /// <summary>Creates an input targeting a lane centre.</summary>
        /// <param name="lane">Zero-based lane index, left to right.</param>
        /// <returns>The input.</returns>
        public static RunnerInput ToLane(int lane) => new RunnerInput(RunnerTuning.LaneCenterX(lane));

        /// <summary>Folds the input into a determinism fingerprint.</summary>
        /// <param name="hash">The accumulator to fold into.</param>
        /// <returns>The updated accumulator.</returns>
        public Hash Fold(Hash hash) => hash.Add(TargetX);

        /// <inheritdoc />
        public bool Equals(RunnerInput other) => TargetX == other.TargetX;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is RunnerInput other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => TargetX.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => "->" + TargetX.ToString();
    }
}

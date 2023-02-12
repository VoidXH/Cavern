using System;
using System.Numerics;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Virtualizer {
    /// <summary>
    /// Handles distancing calculations for a single source's two ears.
    /// </summary>
    public sealed partial class Distancer {
        /// <summary>
        /// The left ear's gain that corresponds to the <see cref="source"/>'s distance.
        /// </summary>
        public float LeftGain { get; private set; }

        /// <summary>
        /// The left ear's gain that corresponds to the <see cref="source"/>'s distance.
        /// </summary>
        public float RightGain { get; private set; }

        /// <summary>
        /// Apply physically correct gain correction. Disable this feature to be able to use a custom gain by distance.
        /// </summary>
        public bool trueGain = true;

        /// <summary>
        /// Decreases real distances by this factor to shrink the environment's scale.
        /// </summary>
        public float distanceFactor;

        /// <summary>
        /// The filtered source.
        /// </summary>
        readonly Source source;

        /// <summary>
        /// The filter processing the <see cref="source"/>.
        /// </summary>
        readonly SpikeConvolver filter;

        /// <summary>
        /// The maximum length of any of the <see cref="impulses"/>, because if the <see cref="FastConvolver"/> is used,
        /// the arrays won't be reassigned and the filter won't cut out, and if the <see cref="SpikeConvolver"/> is used,
        /// the overhead is basically zero.
        /// </summary>
        readonly int filterSize;

        /// <summary>
        /// Create a distance simulation for a <see cref="Source"/>.
        /// </summary>
        public Distancer(Source source) {
            this.source = source;

            // Add the delays to the impulses that were removed for storage optimization
            if (impulseDelays[0][0] != 0) {
                for (int i = 0; i < impulses.Length; ++i) {
                    for (int j = 0; j < impulses[i].Length; ++j) {
                        int convLength = impulses[i][j].Length;
                        short delay = impulseDelays[i][j];
                        Array.Resize(ref impulses[i][j], convLength + delay);
                        Array.Copy(impulses[i][j], 0, impulses[i][j], delay, convLength);
                        Array.Clear(impulses[i][j], 0, convLength);
                    }
                }
                impulseDelays[0][0] = 0;
            }

            source.VolumeRolloff = Rolloffs.Disabled;
            for (int i = 0; i < impulses.Length; ++i) {
                for (int j = 0; j < impulses[i].Length; ++j) {
                    if (filterSize < impulses[i][j].Length) {
                        filterSize = impulses[i][j].Length;
                    }
                }
            }
            distanceFactor = Math.Max(Math.Max(Listener.EnvironmentSize.X, Listener.EnvironmentSize.Y), Listener.EnvironmentSize.Z);
            filter = new SpikeConvolver(new float[filterSize], 0);
        }

        /// <summary>
        /// Generate the left/right ear filters.
        /// </summary>
        /// <param name="right">The object is to the right of the <see cref="Listener"/>'s forward vector</param>
        /// <param name="samples">Single-channel downmixed samples to process</param>
        public void Generate(bool right, float[] samples) {
            float dirMul = -90;
            if (right) {
                dirMul = 90;
            }
            Vector3 sourceForward = new Vector3(0, dirMul, 0).RotateInverse(source.listener.Rotation).PlaceInSphere(),
                dir = source.Position - source.listener.Position;
            float distance = dir.Length(),
                rawAngle = (float)Math.Acos(Vector3.Dot(sourceForward, dir) / distance),
                angle = rawAngle * VectorExtensions.Rad2Deg;
            distance /= distanceFactor;

            // Find bounding angles with discrete impulses
            int smallerAngle = 0;
            while (smallerAngle < angles.Length && angles[smallerAngle] < angle) {
                ++smallerAngle;
            }
            if (smallerAngle != 0) {
                --smallerAngle;
            }
            int largerAngle = smallerAngle + 1;
            if (largerAngle == angles.Length) {
                largerAngle = angles.Length - 1;
            }
            float angleRatio = Math.Min(QMath.LerpInverse(angles[smallerAngle], angles[largerAngle], angle), 1);

            // Find bounding distances with discrete impulses
            int smallerDistance = 0;
            while (smallerDistance < distances.Length && distances[smallerDistance] < distance) {
                ++smallerDistance;
            }
            if (smallerDistance != 0) {
                --smallerDistance;
            }
            int largerDistance = smallerDistance + 1;
            if (largerDistance == distances.Length) {
                largerDistance = distances.Length - 1;
            }
            float distanceRatio =
                Math.Clamp(QMath.LerpInverse(distances[smallerDistance], distances[largerDistance], distance), 0, 1);

            // Find impulse candidates and their weight
            float[][] candidates = new float[][] {
                impulses[smallerAngle][smallerDistance],
                impulses[smallerAngle][largerDistance],
                impulses[largerAngle][smallerDistance],
                impulses[largerAngle][largerDistance]
            };
            float[] gains = {
                (float)Math.Sqrt((1 - angleRatio) * (1 - distanceRatio)),
                (float)Math.Sqrt((1 - angleRatio) * distanceRatio),
                (float)Math.Sqrt(angleRatio * (1 - distanceRatio)),
                (float)Math.Sqrt(angleRatio * distanceRatio)
            };

            // Apply the ear canal's response
            Array.Clear(filter.Impulse, 0, filterSize);
            for (int candidate = 0; candidate < candidates.Length; ++candidate) {
                WaveformUtils.Mix(candidates[candidate], filter.Impulse, gains[candidate]);
            }
            filter.Process(samples);

            // Apply gains
            float angleDiff = MathF.Sin(rawAngle) * .097f;
            float ratioDiff = (distance + angleDiff) * (VirtualizerFilter.ReferenceDistance - angleDiff) /
                             ((distance - angleDiff) * (VirtualizerFilter.ReferenceDistance + angleDiff));
            ratioDiff *= ratioDiff;
            if (right) {
                if (ratioDiff < 1) {
                    RightGain = ratioDiff;
                } else {
                    LeftGain = 1 / ratioDiff;
                }
            } else {
                if (ratioDiff < 1) {
                    LeftGain = ratioDiff;
                } else {
                    RightGain = 1 / ratioDiff;
                }
            }

            if (!trueGain) {
                float powerOffset = 1 / MathF.Sqrt(LeftGain * LeftGain + RightGain * RightGain);
                LeftGain *= powerOffset;
                RightGain *= powerOffset;
            }
        }
    }
}
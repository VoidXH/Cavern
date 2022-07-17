using System;
using System.Runtime.CompilerServices;

namespace Cavern {
    public partial class Source {
        /// <summary>
        /// Logarithmic rolloff by distance.
        /// </summary>
        float RolloffLogarithmic() {
            if (distance > 1) {
                return 1 / (1 + (float)Math.Log(distance));
            }
            return 1;
        }

        /// <summary>
        /// Linear rolloff in range.
        /// </summary>
        float RolloffLinear() => (listener.Range - distance) / listener.Range;

        /// <summary>
        /// Physically correct rolloff by distance.
        /// </summary>
        float RolloffReal() {
            if (distance > 1) {
                return 1 / distance;
            }
            return 1;
        }

        /// <summary>
        /// No rolloff.
        /// </summary>
        static float RolloffDisabled() => 1;

        /// <summary>
        /// Rolloff calculator function.
        /// </summary>
        delegate float RolloffFunc();

        /// <summary>
        /// The rolloff function to be used.
        /// </summary>
        RolloffFunc UsedRolloffFunc = RolloffDisabled;

        /// <summary>
        /// Last value of <see cref="VolumeRolloff"/>.
        /// </summary>
        Rolloffs LastRolloff = Rolloffs.Disabled;

        /// <summary>
        /// Set the <see cref="UsedRolloffFunc"/> to match the user's <see cref="VolumeRolloff"/> setting.
        /// </summary>
        void SetRolloff() {
            UsedRolloffFunc = (LastRolloff = VolumeRolloff) switch {
                Rolloffs.Logarithmic => RolloffLogarithmic,
                Rolloffs.Linear => RolloffLinear,
                Rolloffs.Real => RolloffReal,
                _ => RolloffDisabled,
            };
        }

        /// <summary>
        /// Get the gain by rolloff mode and distance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float GetRolloff() {
            if (LastRolloff != VolumeRolloff) {
                SetRolloff();
            }
            return UsedRolloffFunc.Invoke();
        }
    }
}
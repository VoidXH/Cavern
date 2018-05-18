using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cavern {
    public partial class AudioSource3D : MonoBehaviour {
        /// <summary>Logarithmic rolloff by distance.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float RolloffLogarithmic() {
            return Distance < 1 ? 1 : 1 / (1 + Mathf.Log(Distance));
        }

        /// <summary>Linear rolloff in range.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float RolloffLinear() {
            float Range = AudioListener3D.Current.Range;
            return (Range - Distance) / Range;
        }

        /// <summary>Physically correct rolloff by distance.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float RolloffReal() {
            return Distance < 1 ? 1 : 1 / Distance;
        }

        /// <summary>No rolloff.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float RolloffDisabled() {
            return 1;
        }

        /// <summary>Rolloff calculator function.</summary>
        delegate float RolloffFunc();

        /// <summary>The rolloff function to be used.</summary>
        RolloffFunc UsedRolloffFunc;

        /// <summary>Last value of <see cref="VolumeRolloff"/>.</summary>
        Rolloffs LastRolloff;

        /// <summary>Set the <see cref="UsedRolloffFunc"/> to match the user's <see cref="VolumeRolloff"/> setting.</summary>
        void SetRolloff() {
            switch (LastRolloff = VolumeRolloff) {
                case Rolloffs.Logarithmic:
                    UsedRolloffFunc = RolloffLogarithmic;
                    break;
                case Rolloffs.Linear:
                    UsedRolloffFunc = RolloffLinear;
                    break;
                case Rolloffs.Real:
                    UsedRolloffFunc = RolloffReal;
                    break;
                default:
                    UsedRolloffFunc = RolloffDisabled;
                    break;
            }
        }

        /// <summary>Get the gain by rolloff mode and distance.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float GetRolloff() {
            if (LastRolloff != VolumeRolloff)
                SetRolloff();
            return UsedRolloffFunc.Invoke();
        }
    }
}
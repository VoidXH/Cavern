using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Filters;

namespace Cavern.FilterInterfaces {
    /// <summary>
    /// Creates an impulse response simulation between a <see cref="SimulationSource"/> and this object.
    /// </summary>
    [AddComponentMenu("Audio/Filters/Advanced/Path Simulation Target")]
    public class SimulationTarget : AudioSource3D {
        /// <summary>
        /// Maximal sound travel time in samples, size of the convolution filter.
        /// </summary>
        [Tooltip("Maximal sound travel time in samples, size of the convolution filter.")]
        public int MaxSamples = 1024;

        /// <summary>
        /// The impulse response of the simulated material. A single channel is required with the system sample rate.
        /// </summary>
        [Tooltip("The impulse response of the simulated material. A single channel is required with the system sample rate.")]
        public AudioClip Characteristics;

        /// <summary>
        /// The impulse response of the simulated material in Cavern's format. A single channel is required with the system sample rate.
        /// </summary>
        public Clip Characteristics3D;

        /// <summary>
        /// Convolution filter to apply the character.
        /// </summary>
        Convolver filter;
        /// <summary>
        /// Extracted samples from <see cref="Characteristics"/>.
        /// </summary>
        float[] character;

        /// <summary>
        /// Last generated FIR filter of the echo.
        /// </summary>
        public float[] Impulse { get; internal set; }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() {
            if (Characteristics != null) {
                character = new float[Characteristics.samples];
                Characteristics.GetData(character, 0);
            }
            if (Characteristics3D != null) {
                character = new float[Characteristics3D.Samples];
                Characteristics3D.GetData(character, 0);
            }
        }

        /// <summary>
        /// Prepare this target for an <see cref="Impulse"/> response update.
        /// </summary>
        public void Prepare() => Impulse = new float[MaxSamples];

        /// <summary>
        /// Finalize a freshly generated impulse response.
        /// </summary>
        public void UpdateFilter() {
            if (character != null) {
                new Convolver(character, 0).Process(Impulse);
            }
            filter.Impulse = Impulse;
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnEnable() {
            float[] impulse = new float[MaxSamples];
            impulse[0] = 1;
            filter = new Convolver(impulse, 0);
            AddFilter(filter);
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() => RemoveFilter(filter);
    }
}
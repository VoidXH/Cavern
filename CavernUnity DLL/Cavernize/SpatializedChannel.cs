﻿using System;
using UnityEngine;

using Cavern.Filters;
using Cavern.Helpers;
using Cavern.Utilities;

namespace Cavern.Cavernize {
    /// <summary>All the data <see cref="Cavernizer"/> needs for a single channel.</summary>
    class SpatializedChannel {
        /// <summary>Channel position and type information.</summary>
        public readonly CavernizeChannel Channel;
        /// <summary>Crossover to split the moving and ground part.</summary>
        public readonly Crossover Filter;
        /// <summary>Samples to split between <see cref="MovingSource"/> and <see cref="GroundSource"/>.</summary>
        public readonly float[] Output;
        /// <summary>There is available output data, and the channel should be rendered.</summary>
        public bool WrittenOutput;
        /// <summary>High frequency data with height information.</summary>
        public CavernizeOutput MovingSource { get; private set; }
        /// <summary>Low frequency data that stays on the ground.</summary>
        public CavernizeOutput GroundSource { get; private set; }
        /// <summary>Last low frequency sample (used in the height calculation algorithm).</summary>
        public float LastLow;
        /// <summary>Last unmodified sample (used in the height calculation algorithm).</summary>
        public float LastNormal;
        /// <summary>Last high frequency sample (used in the height calculation algorithm).</summary>
        public float LastHigh;
        /// <summary>The moving part's normalized height from the ground.</summary>
        public float Height;
        /// <summary>Times the sources called for a set of samples. Numbers >= 2 indicate that the next frame should be mixed.</summary>
        public int TicksTook;
        /// <summary>Renderer for <see cref="MovingSource"/>.</summary>
        public Renderer MovingRenderer { get; private set; }
        /// <summary>Renderer for <see cref="GroundSource"/>.</summary>
        public Renderer GroundRenderer { get; private set; }

        void CreateSource(Cavernizer master, bool groundLevel) {
            GameObject newObject;
            if (Channel != CavernizeChannel.ScreenLFE)
                newObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            else
                newObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newObject.name = Channel.Name;
            CavernizeOutput newSource = newObject.AddComponent<CavernizeOutput>();
            newSource.Master = master;
            newSource.Channel = this;
            if (newSource.GroundLevel = groundLevel) {
                GroundSource = newSource;
                GroundRenderer = newObject.GetComponent<Renderer>();
            } else {
                MovingSource = newSource;
                MovingRenderer = newObject.GetComponent<Renderer>();
            }
            newSource.Loop = true;
            newSource.VolumeRolloff = Rolloffs.Disabled;
            newSource.LFE = Channel.LFE;
            // TODO: make it work with custom sources
            //newObject.AddComponent<ScaleByGain>().Source = NewSource;
            if (Channel.Muted)
                newSource.Volume = 0;
            newObject.transform.SetParent(master.transform);
            newObject.transform.localPosition =
                Vector3.Scale(CavernUtilities.VectorMatch(Utils.PlaceInCube(new Vector(0, Channel.Y))), AudioListener3D.EnvironmentSize);
        }

        public SpatializedChannel(CavernizeChannel source, Cavernizer master, int updateRate) {
            Channel = source;
            Filter = new Crossover(AudioListener3D.Current.SampleRate, 250);
            Output = new float[updateRate];
            CreateSource(master, true);
            CreateSource(master, false);
        }

        public void Tick(float effectMult, float smoothFactor, float crossoverFreq, bool visualize) {
            int samples = Output.Length;
            if (!WrittenOutput)
                Array.Clear(Output, 0, samples);
            if (Filter.Frequency != crossoverFreq)
                Filter.Frequency = crossoverFreq;
            Filter.Process(Output);
            MovingRenderer.enabled = GroundRenderer.enabled = visualize && WrittenOutput;
            if (WrittenOutput) {
                float maxDepth = .0001f, maxHeight = .0001f;
                for (int offset = 0; offset < samples; ++offset) {
                    // Height is generated by a simplified measurement of volume and pitch
                    LastHigh = .9f * (LastHigh + Output[offset] - LastNormal);
                    float absHigh = Math.Abs(LastHigh);
                    if (maxHeight < absHigh)
                        maxHeight = absHigh;
                    LastLow = LastLow * .99f + LastHigh * .01f;
                    float absLow = Math.Abs(LastLow);
                    if (maxDepth < absLow)
                        maxDepth = absLow;
                    LastNormal = Output[offset];
                }
                maxHeight = (maxHeight - maxDepth * 1.2f) * effectMult;
                if (maxHeight < -.2f)
                    maxHeight = -.2f;
                else if (maxHeight > 1)
                    maxHeight = 1;
                Height = Utils.Lerp(Height, maxHeight, smoothFactor);
                Transform targetTransform = MovingSource.transform;
                Vector3 oldPos = targetTransform.localPosition;
                targetTransform.localPosition = CavernUtilities.FastLerp(oldPos,
                    new Vector3(oldPos.x, maxHeight * AudioListener3D.EnvironmentSize.y, oldPos.z), smoothFactor);
            }
        }

        public void Destroy() {
            if (MovingSource)
                UnityEngine.Object.Destroy(MovingSource.gameObject);
            if (GroundSource)
                UnityEngine.Object.Destroy(GroundSource.gameObject);
        }
    }
}
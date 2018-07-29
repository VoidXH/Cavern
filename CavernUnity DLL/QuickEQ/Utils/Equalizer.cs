using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Equalizer data collector and exporter.</summary>
    public class Equalizer {
        /// <summary>A single equalizer band.</summary>
        public struct Band {
            /// <summary>Position of the band.</summary>
            public float Frequency;
            /// <summary>Gain at <see cref="Frequency"/> in dB.</summary>
            public float Gain;

            /// <summary>EQ band constructor.</summary>
            public Band(float Frequency, float Gain) {
                this.Frequency = Frequency;
                this.Gain = Gain;
            }
        }

        /// <summary>Bands that make up this equalizer.</summary>
        public IReadOnlyList<Band> Bands { get { return _Bands; } }
        List<Band> _Bands = new List<Band>();

        /// <summary>Add a new band to the EQ.</summary>
        public void AddBand(Band NewBand) {
            _Bands.Add(NewBand);
        }

        /// <summary>Shows the EQ curve in a logarithmically scaled frequency axis.</summary>
        /// <param name="StartFreq">Frequency at the beginning of the curve</param>
        /// <param name="EndFreq">Frequency at the end of the curve</param>
        /// <param name="Length">Points on the curve</param>
        public float[] Visualize(float StartFreq, float EndFreq, int Length) {
            float[] Result = new float[Length];
            int BandCount = _Bands.Count;
            if (BandCount == 0)
                return Result;
            float StartPow = Mathf.Log10(StartFreq), EndPow = Mathf.Log10(EndFreq), PowRange = (EndPow - StartPow) / Length;
            int NextBand = 0, LastBand = 0;
            for (int CurBand = 0, End = BandCount; CurBand < End; ++CurBand) {
                if (_Bands[CurBand].Frequency > StartFreq) {
                    NextBand = CurBand;
                    LastBand = CurBand != 0 ? CurBand - 1 : 0;
                    break;
                }
            }
            float FreqDiffStart = _Bands[LastBand].Frequency, FreqDiff = _Bands[NextBand].Frequency - FreqDiffStart,
                GainDiffStart = _Bands[LastBand].Gain, GainDiff = _Bands[NextBand].Gain - GainDiffStart;
            for (int Pos = 0; Pos < Length; ++Pos) {
                float FreqHere = Mathf.Pow(10, StartPow + PowRange * Pos);
                if (FreqHere > _Bands[NextBand].Frequency) {
                    LastBand = NextBand++;
                    if (NextBand == BandCount) {
                        for (; Pos < Length; ++Pos)
                            Result[Pos] = _Bands[LastBand].Gain;
                        return Result;
                    }
                    FreqDiffStart = _Bands[LastBand].Frequency;
                    FreqDiff = _Bands[NextBand].Frequency - FreqDiffStart;
                    GainDiffStart = _Bands[LastBand].Gain;
                    GainDiff = _Bands[NextBand].Gain - GainDiffStart;
                }
                float FreqPassed = FreqHere - FreqDiffStart;
                if (FreqDiff != 0)
                    Result[Pos] = GainDiffStart + FreqPassed / FreqDiff * GainDiff;
                else
                    Result[Pos] = GainDiffStart;
            }
            return Result;
        }

        /// <summary>Shows the resulting frequency response if this EQ is applied.</summary>
        /// <param name="Response">Frequency response curve to apply the EQ on, from
        /// <see cref="Measurements.ConvertToGraph(float[], float, float, int, int)"/></param>
        /// <param name="StartFreq">Frequency at the beginning of the curve</param>
        /// <param name="EndFreq">Frequency at the end of the curve</param>
        public float[] Apply(float[] Response, float StartFreq, float EndFreq) {
            int Length = Response.Length;
            float[] Filter = Visualize(StartFreq, EndFreq, Length);
            for (int i = 0; i < Length; ++i)
                Filter[i] += Response[i];
            return Filter;
        }

        // TODO: export as configuration file for some EQ applications

        /// <summary>Generate an equalizer setting to flatten the received response.</summary>
        /// <param name="FreqResponse">Frequency response to equalize, must be in decibels (use <see cref="Measurements.ConvertToDecibels(float[])"/>),
        /// and smoothing (<see cref="Measurements.SmoothResponse(float[], float, float, float)"/>) is strongly recommended</param>
        /// <param name="SampleRate">Measurement sampling rate</param>
        /// <param name="ReferenceLevel">Flatten the frequency response to this level</param>
        /// <param name="Resolution">Band diversity in octaves</param>
        /// <param name="MaxGain">Maximum gain of any generated band</param>
        public static Equalizer CorrectResponse(float[] FreqResponse, int SampleRate, float ReferenceLevel, float Resolution = 1 / 3f, float MaxGain = 6) {
            Equalizer Result = new Equalizer();
            int Length = FreqResponse.Length;
            float Nyquist = Length * .5f, FreqPositioner = Nyquist / Length;
            for (int i = 0; i < Length; ++i) {
                float FreqHere = i * FreqPositioner;
                // TODO
            }
            return Result;
        }

        /// <summary>Generate an equalizer setting to flatten the processed response of
        /// <see cref="Measurements.SmoothGraph(float[], float, float, float)"/>.</summary>
        /// <param name="Graph">Graph to equalize, a pre-applied smoothing (<see cref="Measurements.SmoothGraph(float[], float, float, float)"/> is
        /// strongly recommended</param>
        /// <param name="StartFreq">Frequency at the beginning of the graph</param>
        /// <param name="EndFreq">Frequency at the end of the graph</param>
        /// <param name="ReferenceLevel">Flatten the frequency response to this level</param>
        /// <param name="Resolution">Band diversity in octaves</param>
        /// <param name="MaxGain">Maximum gain of any generated band</param>
        public static Equalizer CorrectGraph(float[] Graph, float StartFreq, float EndFreq, float ReferenceLevel, float Resolution = 1 / 3f, float MaxGain = 6) {
            Equalizer Result = new Equalizer();
            int Length = Graph.Length;
            float StartPow = Mathf.Log10(StartFreq), EndPow = Mathf.Log10(EndFreq), PowRange = (EndPow - StartPow) / Length,
                OctaveRange = Mathf.Log(EndFreq, 2) - Mathf.Log(StartFreq, 2), Octaves = OctaveRange / Resolution, MinChecked = ReferenceLevel - MaxGain;
            int WindowSize = Length / (int)Octaves, WindowEdge = WindowSize / 2;
            for (int Pos = 0; Pos < Length; Pos += WindowSize) {
                float CenterFreq = Mathf.Pow(10, StartPow + PowRange * Pos), Average = 0;
                int AverageCount = 0;
                for (int Sample = Math.Max(Pos - WindowEdge, 0), End = Math.Min(Sample + WindowSize, Length); Sample < End; ++Sample) {
                    if (Graph[Sample] > MinChecked) {
                        Average += Graph[Sample];
                        ++AverageCount;
                    }
                }
                if (AverageCount != 0)
                    Result.AddBand(new Band(CenterFreq, ReferenceLevel - Average / AverageCount));
            }
            return Result;
        }
    }
}
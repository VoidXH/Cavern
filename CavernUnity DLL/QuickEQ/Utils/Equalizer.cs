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
            _Bands.Sort((a, b) => a.Frequency.CompareTo(b.Frequency));
        }

        /// <summary>Remove a band from the EQ.</summary>
        public void RemoveBand(Band Removable) {
            _Bands.Remove(Removable);
        }

        /// <summary>Reset this EQ.</summary>
        public void ClearBands() {
            _Bands.Clear();
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

        /// <summary>Generate an equalizer setting to flatten the received response.</summary>
        /// <param name="FreqResponse">Frequency response to equalize, must be in decibels (use <see cref="Measurements.ConvertToDecibels(float[])"/>),
        /// and smoothing (<see cref="Measurements.SmoothResponse(float[], float, float, float)"/>) is strongly recommended, but the only allowed range is 0 to
        /// sample rate / 2</param>
        /// <param name="SampleRate">Measurement sampling rate</param>
        /// <param name="ReferenceCurve">Match the frequency response to this linear curve of any length, one value means a flat response</param>
        /// <param name="Resolution">Band diversity in octaves</param>
        /// <param name="MaxGain">Maximum gain of any generated band</param>
        public static Equalizer CorrectResponse(float[] FreqResponse, int SampleRate, float[] ReferenceCurve, float Resolution = 1 / 3f, float MaxGain = 6) {
            Equalizer Result = new Equalizer();
            int Length = FreqResponse.Length;
            float Nyquist = Length * .5f, Positioner = Nyquist / Length, Offset = Mathf.Pow(2, Resolution), RefPositioner = ReferenceCurve.Length / (float)Length;
            int Steps = (int)(Mathf.Log(Nyquist, 2) / Resolution);
            --Length;
            for (int Band = Steps; Band >= Steps; --Band) {
                float Freq = Mathf.Pow(2, Band * Resolution);
                int RefPos = RefPos = (int)(Freq * Positioner * RefPositioner);
                float WindowStart = Freq / Offset, WindowEnd = Freq * Offset, Average = 0, MinChecked = ReferenceCurve[RefPos] - MaxGain;
                int Start = (int)(WindowStart * Positioner), End = (int)(WindowEnd * Positioner);
                if (Start < 0) Start = 0;
                if (End > Length) End = Length;
                int AverageCount = 0;
                for (int Sample = Start; Sample <= End; ++Sample) {
                    if (FreqResponse[Sample] > MinChecked) {
                        Average += FreqResponse[Sample];
                        ++AverageCount;
                    }
                }
                if (AverageCount != 0)
                    Result.AddBand(new Band(Freq, ReferenceCurve[RefPos] - Average / AverageCount));
            }
            return Result;
        }

        /// <summary>Generate an equalizer setting to flatten the processed response of
        /// <see cref="Measurements.SmoothGraph(float[], float, float, float)"/>.</summary>
        /// <param name="Graph">Graph to equalize, a pre-applied smoothing (<see cref="Measurements.SmoothGraph(float[], float, float, float)"/> is
        /// strongly recommended</param>
        /// <param name="StartFreq">Frequency at the beginning of the graph</param>
        /// <param name="EndFreq">Frequency at the end of the graph</param>
        /// <param name="ReferenceCurve">Match the frequency response to this logarithmic curve of any length, one value means a flat response</param>
        /// <param name="Resolution">Band diversity in octaves</param>
        /// <param name="MaxGain">Maximum gain of any generated band</param>
        public static Equalizer CorrectGraph(float[] Graph, float StartFreq, float EndFreq, float[] ReferenceCurve, float Resolution = 1 / 3f, float MaxGain = 6) {
            Equalizer Result = new Equalizer();
            int Length = Graph.Length;
            float StartPow = Mathf.Log10(StartFreq), EndPow = Mathf.Log10(EndFreq), PowRange = (EndPow - StartPow) / Length,
                OctaveRange = Mathf.Log(EndFreq, 2) - Mathf.Log(StartFreq, 2), Bands = OctaveRange / Resolution + 1,
                RefPositioner = ReferenceCurve.Length / (float)Length;
            int WindowSize = Length / (int)Bands, WindowEdge = WindowSize / 2;
            for (int Pos = Length - 1; Pos >= 0; Pos -= WindowSize) {
                int AverageCount = 0, RefPos = (int)(Pos * RefPositioner);
                float CenterFreq = Mathf.Pow(10, StartPow + PowRange * Pos), Average = 0, MinChecked = ReferenceCurve[RefPos] - MaxGain;
                int Sample = Pos - WindowEdge, End = Sample + WindowSize;
                if (Sample < 0) Sample = 0;
                if (End > Length) End = Length;
                for (; Sample < End; ++Sample) {
                    if (Graph[Sample] > MinChecked) {
                        Average += Graph[Sample];
                        ++AverageCount;
                    }
                }
                if (AverageCount != 0)
                    Result.AddBand(new Band(CenterFreq, ReferenceCurve[RefPos] - Average / AverageCount));
            }
            return Result;
        }
    }
}
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.Debug {
    /// <summary>
    /// Cinema channel array volume meters (7.1 + overhead sides).
    /// </summary>
    [AddComponentMenu("Audio/Debug/Array Levels")]
    public class ArrayLevels : WindowBase {
        /// <summary>
        /// The lowest volume to show (in decibels).
        /// </summary>
        [Tooltip("The lowest volume to show (in decibels).")]
        [Range(-300, -6)]
        public int DynamicRange = -60;

        struct ArrayLevelData {
            public float Peak;
            public Texture2D Color;
        }

        readonly ArrayLevelData[] channelData = new ArrayLevelData[channels];

        Texture2D white;

        /// <summary>
        /// Window dimension, name, and custom variable setup.
        /// </summary>
        protected override void Setup() {
            width = 0;
            height = 170;
            title = "Array Levels";
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
            for (int Channel = 0; Channel < channels; ++Channel) {
                Texture2D ChannelColor = new Texture2D(1, 1);
                ChannelColor.SetPixel(0, 0, channelColors[Channel]);
                ChannelColor.Apply();
                channelData[Channel].Color = ChannelColor;
            }
        }

        const int barHeight = 140;

        /// <summary>
        /// Draw window contents.
        /// </summary>
        /// <param name="wID">Window ID</param>
        protected override void Draw(int wID) {
            int blockWidth = 30, gapWidth = blockWidth / 6, barWidth = blockWidth - gapWidth * 2, targetWidth = channels * blockWidth;
            Position.width = this.width = targetWidth + 30;
            TextAnchor oldAlign = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            int left = 25 + gapWidth, top = 25, width = (int)Position.width - 4;
            int oldSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 12;
            GUI.Label(new Rect(0, top, 30, 14), (DynamicRange / 10).ToString());
            for (int i = 2; i <= 10; ++i) {
                GUI.Label(new Rect(0, top += 14, 30, 14), (DynamicRange * i / 10).ToString());
                GUI.DrawTexture(new Rect(2, top, width, 1), white);
            }
            GUI.skin.label.fontSize = 10;
            for (int channel = 0; channel < channels; ++channel) {
                float peak = channelData[channel].Peak;
                if (peak > 0) {
                    int height = (int)(peak * barHeight);
                    GUI.DrawTexture(new Rect(left, 165 - height, barWidth, height), channelData[channel].Color);
                }
                GUI.Label(new Rect(left, 150, barWidth, 15), markers[channel]);
                left += blockWidth;
            }
            GUI.skin.label.fontSize = oldSize;
            GUI.skin.label.alignment = oldAlign;
            GUI.DragWindow();
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            float[] arrayLevels = new float[channels], outputCache = AudioListener3D.Output;
            for (int actChannel = 0, multichannelUpdateRate = outputCache.Length; actChannel < Listener.Channels.Length; ++actChannel) {
                float max = 0, absSample;
                for (int sample = actChannel; sample < multichannelUpdateRate; sample += Listener.Channels.Length) {
                    absSample = Math.Abs(outputCache[sample]);
                    if (max < absSample) {
                        max = absSample;
                    }
                }
                Channel thisChannel = Listener.Channels[actChannel];
                if (!thisChannel.LFE) {
                    if (thisChannel.X >= 0) { // Standard 7.1 and below
                        if (thisChannel.Y % 180 == 0) {
                            if (thisChannel.Y == 0) {
                                arrayLevels[center] += max;
                            } else {
                                max *= .5f;
                                arrayLevels[rearL] += max;
                                arrayLevels[rearR] += max;
                            }
                        } else if (thisChannel.Y < 0) {
                            if (thisChannel.Y < -45) {
                                arrayLevels[thisChannel.Y < -135 ? rearL : surroundL] += max;
                            } else {
                                arrayLevels[frontL] += max;
                            }
                        } else if (thisChannel.Y > 0) {
                            if (thisChannel.Y > 45) {
                                arrayLevels[thisChannel.Y > 135 ? rearR : surroundR] += max;
                            } else {
                                arrayLevels[frontR] += max;
                            }
                        }
                    } else { // Height/overhead channels
                        if (thisChannel.Y < 0) {
                            arrayLevels[topL] += max;
                        } else if (thisChannel.Y > 0) {
                            arrayLevels[topR] += max;
                        } else {
                            max *= .5f;
                            arrayLevels[topL] += max;
                            arrayLevels[topR] += max;
                        }
                    }
                } else {
                    arrayLevels[LFE] += max;
                }
            }
            for (int channel = 0; channel < channels; ++channel) {
                if (arrayLevels[channel] > 1) {
                    arrayLevels[channel] = 1;
                }
                float currentLevel = 20 * Mathf.Log10(arrayLevels[channel]) / -DynamicRange + 1,
                    currentPeak = channelData[channel].Peak - Time.deltaTime;
                if (currentPeak < currentLevel) {
                    currentPeak = currentLevel;
                }
                channelData[channel].Peak = currentPeak;
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDestroy() {
            Destroy(white);
            for (int Channel = 0; Channel < channels; ++Channel) {
                Destroy(channelData[Channel].Color);
            }
        }

        const int channels = 10,
            frontL = 0,
            center = 1,
            frontR = 2,
            surroundL = 3,
            surroundR = 4,
            rearL = 5,
            rearR = 6,
            topL = 7,
            topR = 8,
            LFE = 9;

        static readonly string[] markers = { "L", "C", "R", "SL", "SR", "RL", "RR", "TL", "TR", "LFE" };

        static readonly Color[] channelColors = new Color[] {
            ColorUtils.frontJack, ColorUtils.centerJack, ColorUtils.frontJack,
            ColorUtils.sideJack, ColorUtils.sideJack, Color.black, Color.black,
            ColorUtils.CavernBlue, ColorUtils.CavernBlue, ColorUtils.centerJack
        };
    }
}
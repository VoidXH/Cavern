using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.Debug {
    /// <summary>
    /// Current channel volume display window.
    /// </summary>
    [AddComponentMenu("Audio/Debug/Levels")]
    public class Levels : WindowBase {
        /// <summary>
        /// Use PC Jack output coloring for level bars. If false, channels will be colored by grouping.
        /// </summary>
        [Tooltip("Use PC Jack output coloring for level bars. If false, channels will be colored by grouping.")]
        public bool jackColoring = true;

        /// <summary>
        /// The lowest volume to show (in decibels).
        /// </summary>
        [Tooltip("The lowest volume to show (in decibels).")]
        [Range(-300, -6)] public int DynamicRange = -60;

        /// <summary>
        /// Maximum width of the Levels window. 0 means the screen's width.
        /// </summary>
        [Tooltip("Maximum width of the Levels window. Non-positive numbers mean the screen's width.")]
        public int MaxWidth;

        /// <summary>
        /// Alias for <see cref="jackColoring"/> to be used with Unity Events.
        /// </summary>
        public bool JackColoring {
            get => jackColoring;
            set => jackColoring = value;
        }

        struct ChannelLevelData {
            public float Peak;
            public Texture2D Color;
            public Vector3 LastPos;
        }

        ChannelLevelData[] channelData = new ChannelLevelData[0];

        bool oldJackColoring = true;

        Texture2D white;

        /// <summary>
        /// Window dimension, name, and custom variable setup.
        /// </summary>
        protected override void Setup() {
            width = 0;
            height = 170;
            title = "Levels";
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
        }

        /// <summary>
        /// Create a new <see cref="ChannelLevelData"/> for each existing channels, and use the user-set color scheme.
        /// </summary>
        void RepaintChannels() {
            if (channelData.Length != Listener.Channels.Length) {
                channelData = new ChannelLevelData[Listener.Channels.Length];
                for (int channel = 0; channel < Listener.Channels.Length; channel++) {
                    Destroy(channelData[channel].Color);
                }
            }
            for (int channel = 0; channel < Listener.Channels.Length; channel++) {
                if (channelData[channel].Color == null) {
                    channelData[channel].Color = new Texture2D(1, 1);
                }
                channelData[channel].LastPos = VectorUtils.VectorMatch(Listener.Channels[channel].CubicalPos);
                channelData[channel].Color.SetPixel(0, 0, ColorUtils.GetChannelColor(channel, jackColoring));
                channelData[channel].Color.Apply();
            }
            oldJackColoring = JackColoring;
        }

        /// <summary>
        /// Draw window contents.
        /// </summary>
        /// <param name="wID">Window ID</param>
        protected override void Draw(int wID) {
            int maximumWidth = (MaxWidth <= 0 ? Screen.width : MaxWidth) - 30,
                blockWidth = Math.Min(maximumWidth / Listener.Channels.Length, 30),
                gapWidth = blockWidth / 6,
                barWidth = blockWidth - gapWidth * 2,
                targetWidth = Listener.Channels.Length * blockWidth;
            Position.width = this.width = targetWidth + 30;
            TextAnchor oldAlign = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            if (channelData.Length != Listener.Channels.Length) {
                RepaintChannels();
            }
            int left = 25 + gapWidth,
                top = 25,
                width = (int)Position.width - 4,
                oldSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 12;
            GUI.Label(new Rect(0, top, 30, 14), (DynamicRange / 10).ToString());
            for (int i = 2; i <= 10; i++) {
                GUI.Label(new Rect(0, top += 14, 30, 14), (DynamicRange * i / 10).ToString());
                GUI.DrawTexture(new Rect(2, top, width, 1), white);
            }
            for (int channel = 0; channel < Listener.Channels.Length; channel++) {
                float peak = channelData[channel].Peak;
                if (peak > 0) {
                    int height = (int)(peak * 140);
                    GUI.DrawTexture(new Rect(left, 165 - height, barWidth, height), channelData[channel].Color);
                }
                GUI.Label(new Rect(left, 150, barWidth, 15), (channel + 1).ToString());
                left += blockWidth;
            }
            GUI.skin.label.fontSize = oldSize;
            GUI.skin.label.alignment = oldAlign;
            GUI.DragWindow();
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            int channels = Listener.Channels.Length;
            if (channelData.Length != channels || JackColoring != oldJackColoring) {
                RepaintChannels();
            }
            bool doRepaint = false;
            float[] outputCache = AudioListener3D.Output;
            if (outputCache == null) {
                return;
            }
            for (int channel = 0; channel < channels; channel++) {
                float max = 0, absSample;
                for (int sample = channel; sample < outputCache.Length; sample += channels) {
                    absSample = Math.Abs(outputCache[sample]);
                    if (max < absSample) {
                        max = absSample;
                    }
                }
                float currentBarHeight = 20 * Mathf.Log10(max) / -DynamicRange + 1, currentPeak = channelData[channel].Peak - Time.deltaTime;
                if (currentPeak < currentBarHeight) {
                    currentPeak = currentBarHeight;
                }
                channelData[channel].Peak = currentPeak;
                doRepaint |= VectorUtils.VectorCompare(Listener.Channels[channel].CubicalPos, channelData[channel].LastPos);
            }
            if (doRepaint) {
                RepaintChannels();
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDestroy() {
            Destroy(white);
            for (int channel = 0; channel < channelData.Length; channel++) {
                Destroy(channelData[channel].Color);
            }
        }
    }
}
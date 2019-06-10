using System;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.Debug {
    /// <summary>Current channel volume display window.</summary>
    [AddComponentMenu("Audio/Debug/Levels")]
    public class Levels : WindowBase {
        /// <summary>Use PC Jack output coloring for level bars. If false, channels will be colored by grouping.</summary>
        [Tooltip("Use PC Jack output coloring for level bars. If false, channels will be colored by grouping.")]
        public bool JackColoring = true;
        /// <summary>The lowest volume to show (in decibels).</summary>
        [Tooltip("The lowest volume to show (in decibels).")]
        [Range(-300, -6)] public int DynamicRange = -60;
        /// <summary>Maximum width of the Levels window. 0 means the screen's width.</summary>
        [Tooltip("Maximum width of the Levels window. Non-positive numbers mean the screen's width.")]
        public int MaxWidth = 0;

        struct ChannelLevelData {
            public float Peak;
            public Texture2D Color;
            public Vector3 LastPos;
        }

        ChannelLevelData[] channelData = new ChannelLevelData[0];

        bool oldJackColoring = true;
        Texture2D white;

        /// <summary>Window dimension, name, and custom variable setup.</summary>
        protected override void Setup() {
            width = 0;
            height = 170;
            title = "Levels";
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
        }

        /// <summary>Get a color by hue value.</summary>
        /// <param name="degrees">Hue value in degrees.</param>
        static Color GetHueColor(float degrees) {
            degrees %= 360;
            if (degrees < 0)
                degrees += 360f;
            return degrees < 60 ? new Color(1f, degrees / 60f, 0f) :
                degrees < 120 ? new Color(1f - (degrees - 60f) / 60f, 1f, 0f) :
                degrees < 180 ? new Color(0, 1f, (degrees - 120f) / 60f) :
                degrees < 240 ? new Color(0, 1f - (degrees - 180f) / 60f, 1f) :
                degrees < 300 ? new Color((degrees - 240f) / 60f, 0f, 1f) :
                new Color(1f, 0f, 1f - (degrees - 300f) / 60f);
        }

        /// <summary>Create a new <see cref="ChannelLevelData"/> for each existing channels, and use the user-set color scheme.</summary>
        void RepaintChannels() {
            int channels = Listener.Channels.Length;
            if (channelData.Length != channels) {
                channelData = new ChannelLevelData[channels];
                for (int channel = 0; channel < channels; ++channel)
                    Destroy(channelData[channel].Color);
            }
            for (int channel = 0; channel < channels; ++channel) {
                if (channelData[channel].Color == null)
                    channelData[channel].Color = new Texture2D(1, 1);
                channelData[channel].LastPos = CavernUtilities.VectorMatch(AudioListener3D.Channels[channel].CubicalPos);
                if (JackColoring) {
                    channelData[channel].Color.SetPixel(0, 0, channel < 2 ? new Color(.596078431f, .984313725f, .596078431f, 1) :
                        channel < 4 ? (channels <= 4 ? Color.black : new Color(1, .647058824f, 0, 1)) :
                        channel < 6 ? Color.black :
                        channel < 8 ? new Color(.75294117647f, .75294117647f, .75294117647f, 1) :
                        new Color(0, .578125f, .75f, 1));
                } else {
                    Vector3 channelPos = channelData[channel].LastPos;
                    Color targetColor = AudioListener3D.Channels[channel].LFE ? Color.black :
                        GetHueColor(channelPos.z % 1 == 0 ? (channelPos.y + 1f) * channelPos.z * 45f + 180f : (channelPos.x * (channelPos.y + 1f) * 22.5f + 45f));
                    targetColor = new Color(targetColor.r * .75f + .25f, targetColor.g * .75f + .25f, targetColor.b * .75f + .25f);
                    channelData[channel].Color.SetPixel(0, 0, targetColor);
                }
                channelData[channel].Color.Apply();
            }
            oldJackColoring = JackColoring;
        }

        /// <summary>Draw window contents.</summary>
        /// <param name="wID">Window ID</param>
        protected override void Draw(int wID) {
            int maximumWidth = (MaxWidth <= 0 ? Screen.width : MaxWidth) - 30, channels = Listener.Channels.Length, blockWidth = Math.Min(maximumWidth / channels, 30),
                gapWidth = blockWidth / 6, barWidth = blockWidth - gapWidth * 2, targetWidth = channels * blockWidth;
            Position.width = this.width = targetWidth + 30;
            TextAnchor oldAlign = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            if (channelData.Length != channels)
                RepaintChannels();
            int left = 25 + gapWidth, top = 25, width = (int)Position.width - 4;
            int oldSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 12;
            GUI.Label(new Rect(0, top, 30, 14), (DynamicRange / 10).ToString());
            for (int i = 2; i <= 10; ++i) {
                GUI.Label(new Rect(0, top += 14, 30, 14), (DynamicRange * i / 10).ToString());
                GUI.DrawTexture(new Rect(2, top, width, 1), white);
            }
            for (int channel = 0; channel < channels; ++channel) {
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

        void Update() {
            int channels = Listener.Channels.Length;
            if (channelData.Length != channels || JackColoring != oldJackColoring)
                RepaintChannels();
            bool doRepaint = false;
            float[] outputCache = AudioListener3D.Output;
            if (outputCache == null)
                return;
            for (int channel = 0, samples = outputCache.Length; channel < channels; ++channel) {
                float max = 0;
                for (int sample = channel; sample < samples; sample += channels) {
                    float AbsSample = Math.Abs(outputCache[sample]);
                    if (max < AbsSample)
                        max = AbsSample;
                }
                float currentBarHeight = CavernUtilities.SignalToDb(max) / -DynamicRange + 1, currentPeak = channelData[channel].Peak - Time.deltaTime;
                if (currentPeak < currentBarHeight)
                    currentPeak = currentBarHeight;
                channelData[channel].Peak = currentPeak;
                doRepaint |= CavernUtilities.VectorCompare(AudioListener3D.Channels[channel].CubicalPos, channelData[channel].LastPos);
            }
            if (doRepaint)
                RepaintChannels();
        }

        void OnDestroy() {
            Destroy(white);
            for (int channel = 0, Channels = channelData.Length; channel < Channels; ++channel)
                Destroy(channelData[channel].Color);
        }
    }
}
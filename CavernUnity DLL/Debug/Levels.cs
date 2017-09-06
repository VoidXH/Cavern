using System;
using UnityEngine;
using Cavern.Helpers;

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
        }

        ChannelLevelData[] ChannelData = new ChannelLevelData[0];

        bool OldJackColoring = true;
        Texture2D White;

        /// <summary>Window dimension, name, and custom variable setup.</summary>
        protected override void Setup() {
            Width = 0;
            Height = 170;
            Title = "Levels";
            White = new Texture2D(1, 1);
            White.SetPixel(0, 0, Color.white);
            White.Apply();
        }

        /// <summary>Get a color by hue value.</summary>
        /// <param name="Degrees">Hue value in degrees.</param>
        static Color GetHueColor(float Degrees) {
            Degrees %= 360;
            if (Degrees < 0)
                Degrees += 360f;
            return Degrees < 60 ? new Color(1f, Degrees / 60f, 0f) :
                Degrees < 120 ? new Color(1f - (Degrees - 60f) / 60f, 1f, 0f) :
                Degrees < 180 ? new Color(0, 1f, (Degrees - 120f) / 60f) :
                Degrees < 240 ? new Color(0, 1f - (Degrees - 180f) / 60f, 1f) :
                Degrees < 300 ? new Color((Degrees - 240f) / 60f, 0f, 1f) :
                new Color(1f, 0f, 1f - (Degrees - 300f) / 60f);
        }

        /// <summary>Create a new <see cref="ChannelLevelData"/> for each existing channels, and use the user-set color scheme.</summary>
        void RepaintChannels() {
            int Channels = AudioListener3D.ChannelCount;
            ChannelData = new ChannelLevelData[Channels];
            for (int Channel = 0; Channel < Channels; ++Channel) {
                if (ChannelData[Channel].Color)
                    Destroy(ChannelData[Channel].Color);
                ChannelData[Channel].Color = new Texture2D(1, 1);
                if (JackColoring) {
                    ChannelData[Channel].Color.SetPixel(0, 0, Channel < 2 ? new Color(.596078431f, .984313725f, .596078431f, 1) :
                        Channel < 4 ? (Channels <= 4 ? Color.black : new Color(1, .647058824f, 0, 1)) :
                        Channel < 6 ? Color.black :
                        Channel < 8 ? new Color(.75294117647f, .75294117647f, .75294117647f, 1) :
                        new Color(0, .578125f, .75f, 1));
                } else {
                    Vector3 ChannelPos = AudioListener3D.Channels[Channel].CubicalPos;
                    Color TargetColor = AudioListener3D.Channels[Channel].LFE ? Color.black :
                        GetHueColor(ChannelPos.z % 1 == 0 ? (ChannelPos.y + 1f) * ChannelPos.z * 45f + 180f : (ChannelPos.x * (ChannelPos.y + 1f) * 22.5f + 45f));
                    TargetColor = new Color(TargetColor.r * .75f + .25f, TargetColor.g * .75f + .25f, TargetColor.b * .75f + .25f);
                    ChannelData[Channel].Color.SetPixel(0, 0, TargetColor);
                }
                ChannelData[Channel].Color.Apply();
            }
            OldJackColoring = JackColoring;
        }

        /// <summary>Draw window contents.</summary>
        /// <param name="wID">Window ID</param>
        protected override void Draw(int wID) {
            int MaximumWidth = (MaxWidth <= 0 ? Screen.width : MaxWidth) - 30, Channels = AudioListener3D.ChannelCount, BlockWidth = Math.Min(MaximumWidth / Channels, 30),
                GapWidth = BlockWidth / 6, BarPlusGap = BlockWidth - GapWidth, BarWidth = BarPlusGap - GapWidth, TargetWidth = Channels * BlockWidth;
            Position.width = this.Width = TargetWidth + 30;
            TextAnchor OldAlign = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            if (ChannelData.Length != Channels || JackColoring != OldJackColoring)
                RepaintChannels();
            int Left = 25, Top = 25, Width = (int)Position.width - 4;
            int OldSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 12;
            GUI.Label(new Rect(0, Top, 30, 14), (DynamicRange / 10).ToString());
            for (int i = 2; i <= 10; i++) {
                GUI.Label(new Rect(0, Top += 14, 30, 14), (DynamicRange * i / 10).ToString());
                GUI.DrawTexture(new Rect(2, Top, Width, 1), White);
            }
            GUI.skin.label.fontSize = OldSize;
            int MultichannelUpdateRate = AudioListener3D.Output.Length;
            for (int Channel = 0; Channel < Channels; ++Channel) {
                float Max = 0;
                for (int Sample = Channel; Sample < MultichannelUpdateRate; Sample += Channels)
                    if (Max < AudioListener3D.Output[Sample])
                        Max = AudioListener3D.Output[Sample];
                float CurrentBarHeight = CavernUtilities.SignalToDb(Max) / -DynamicRange + 1, CurrentPeak = ChannelData[Channel].Peak - Time.deltaTime;
                if (CurrentPeak < CurrentBarHeight)
                    CurrentPeak = CurrentBarHeight;
                if (CurrentPeak < 0)
                    CurrentPeak = 0;
                int Height = (int)((ChannelData[Channel].Peak = CurrentPeak) * 140);
                GUI.DrawTexture(new Rect(Left += GapWidth, 165 - Height, BarWidth, Height), ChannelData[Channel].Color);
                Left += BarPlusGap;
            }
            GUI.skin.label.alignment = OldAlign;
            GUI.DragWindow();
        }

        void OnDestroy() {
            Destroy(White);
            int Channels = ChannelData.Length;
            for (int Channel = 0; Channel < Channels; ++Channel)
                Destroy(ChannelData[Channel].Color);
        }
    }
}
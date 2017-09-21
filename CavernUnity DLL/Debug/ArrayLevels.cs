using UnityEngine;

using Cavern.Helpers;

namespace Cavern.Debug {
    /// <summary>Cinema channel array volume meters (7.1 + overhead sides).</summary>
    [AddComponentMenu("Audio/Debug/Array Levels")]
    public class ArrayLevels : WindowBase {
        /// <summary>The lowest volume to show (in decibels).</summary>
        [Tooltip("The lowest volume to show (in decibels).")]
        [Range(-300, -6)]
        public int DynamicRange = -60;

        struct ArrayLevelData {
            public float Peak;
            public Texture2D Color;
        }

        const int Channels = 10,
            Left = 0,
            Center = 1,
            Right = 2,
            SurroundL = 3,
            SurroundR = 4,
            RearL = 5,
            RearR = 6,
            TopL = 7,
            TopR = 8,
            LFE = 9;
        readonly static string[] Markers = new string[] { "L", "C", "R", "SL", "SR", "RL", "RR", "TL", "TR", "LFE" };
        readonly static Color[] ChannelColors = new Color[] {
            new Color(.596078431f, .984313725f, .596078431f, 1),
            new Color(1, .647058824f, 0, 1),
            new Color(.596078431f, .984313725f, .596078431f, 1),
            new Color(.75294117647f, .75294117647f, .75294117647f, 1),
            new Color(.75294117647f, .75294117647f, .75294117647f, 1),
            Color.black,
            Color.black,
            new Color(0, .578125f, .75f, 1),
            new Color(0, .578125f, .75f, 1),
            Color.red
        };

        ArrayLevelData[] ChannelData = new ArrayLevelData[Channels];

        Texture2D White;

        /// <summary>Window dimension, name, and custom variable setup.</summary>
        protected override void Setup() {
            Width = 0;
            Height = 170;
            Title = "Array Levels";
            White = new Texture2D(1, 1);
            White.SetPixel(0, 0, Color.white);
            White.Apply();
            for (int Channel = 0; Channel < Channels; ++Channel) {
                Texture2D ChannelColor = new Texture2D(1, 1);
                ChannelColor.SetPixel(0, 0, ChannelColors[Channel]);
                ChannelColor.Apply();
                ChannelData[Channel].Color = ChannelColor;
            }
        }

        const int BarHeight = 140;

        /// <summary>Draw window contents.</summary>
        /// <param name="wID">Window ID</param>
        protected override void Draw(int wID) {
            int BlockWidth = 30, GapWidth = BlockWidth / 6, BarWidth = BlockWidth - GapWidth * 2, TargetWidth = Channels * BlockWidth;
            Position.width = this.Width = TargetWidth + 30;
            TextAnchor OldAlign = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            int Left = 25 + GapWidth, Top = 25, Width = (int)Position.width - 4;
            int OldSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 12;
            GUI.Label(new Rect(0, Top, 30, 14), (DynamicRange / 10).ToString());
            for (int i = 2; i <= 10; i++) {
                GUI.Label(new Rect(0, Top += 14, 30, 14), (DynamicRange * i / 10).ToString());
                GUI.DrawTexture(new Rect(2, Top, Width, 1), White);
            }
            for (int Channel = 0; Channel < Channels; ++Channel) {
                float Peak = ChannelData[Channel].Peak;
                if (Peak > 0) {
                    int Height = (int)(Peak * BarHeight);
                    GUI.DrawTexture(new Rect(Left, 165 - Height, BarWidth, Height), ChannelData[Channel].Color);
                }
                GUI.Label(new Rect(Left, 150, BarWidth, 15), Markers[Channel]);
                Left += BlockWidth;
            }
            GUI.skin.label.fontSize = OldSize;
            GUI.skin.label.alignment = OldAlign;
            GUI.DragWindow();
        }

        void Update() {
            float[] ArrayLevels = new float[Channels];
            int CavernChannels = AudioListener3D.ChannelCount, MultichannelUpdateRate = AudioListener3D.Output.Length;
            for (int ActChannel = 0; ActChannel < CavernChannels; ++ActChannel) {
                float Max = 0;
                for (int Sample = ActChannel; Sample < MultichannelUpdateRate; Sample += CavernChannels) {
                    float AbsSample = CavernUtilities.Abs(AudioListener3D.Output[Sample]);
                    if (Max < AbsSample)
                        Max = AbsSample;
                }
                Channel ThisChannel = AudioListener3D.Channels[ActChannel];
                if (!ThisChannel.LFE) {
                    if (ThisChannel.x >= 0) { // Standard 7.1 and below
                        if (ThisChannel.y % 180 == 0) {
                            if (ThisChannel.y == 0)
                                ArrayLevels[Center] += Max;
                            else {
                                Max *= .5f;
                                ArrayLevels[RearL] += Max;
                                ArrayLevels[RearR] += Max;
                            }
                        } else if (ThisChannel.y < 0) {
                            if (ThisChannel.y < -45)
                                ArrayLevels[ThisChannel.y < -135 ? RearL : SurroundL] += Max;
                            else
                                ArrayLevels[Left] += Max;
                        } else if (ThisChannel.y > 0) {
                            if (ThisChannel.y > 45)
                                ArrayLevels[ThisChannel.y > 135 ? RearR : SurroundR] += Max;
                            else
                                ArrayLevels[Right] += Max;
                        }
                    } else { // Height/overhead channels
                        if (ThisChannel.y < 0)
                            ArrayLevels[TopL] += Max;
                        else if (ThisChannel.y > 0)
                            ArrayLevels[TopR] += Max;
                        else {
                            Max *= .5f;
                            ArrayLevels[TopL] += Max;
                            ArrayLevels[TopR] += Max;
                        }
                    }
                } else
                    ArrayLevels[LFE] += Max;
            }
            for (int Channel = 0; Channel < Channels; ++Channel) {
                if (ArrayLevels[Channel] > 1)
                    ArrayLevels[Channel] = 1;
                float CurrentBarHeight = CavernUtilities.SignalToDb(ArrayLevels[Channel]) / -DynamicRange + 1, CurrentPeak = ChannelData[Channel].Peak - Time.deltaTime;
                if (CurrentPeak < CurrentBarHeight)
                    CurrentPeak = CurrentBarHeight;
                ChannelData[Channel].Peak = CurrentPeak;
            }
        }

        void OnDestroy() {
            Destroy(White);
            for (int Channel = 0; Channel < Channels; ++Channel)
                Destroy(ChannelData[Channel].Color);
        }
    }
}
using UnityEngine;
using Cavern.Helpers;

namespace Cavern.Debug {
    [AddComponentMenu("Audio/Debug/Levels")]
    public class Levels : WindowBase {
        [Tooltip("The lowest volume to show (in decibels).")]
        [Range(-300, -6)] public int DynamicRange = -60;

        float[] Peaks = new float[0];
        Texture2D Green;
        Texture2D Orange;
        Texture2D Black;
        Texture2D Grey;
        Texture2D Blue;
        Texture2D White;

        static float SignalToDb(float Amplitude) {
            return 20 * Mathf.Log10(Amplitude);
        }

        protected override void Setup() {
            Width = 0;
            Height = 170;
            Title = "Levels";
            Green = new Texture2D(1, 1);
            Green.SetPixel(0, 0, new Color(.596078431f, .984313725f, .596078431f, 1));
            Green.Apply();
            Orange = new Texture2D(1, 1);
            Orange.SetPixel(0, 0, new Color(1, .647058824f, 0, 1));
            Orange.Apply();
            Black = new Texture2D(1, 1);
            Black.SetPixel(0, 0, Color.black);
            Black.Apply();
            Grey = new Texture2D(1, 1);
            Grey.SetPixel(0, 0, new Color(.75294117647f, .75294117647f, .75294117647f, 1));
            Grey.Apply();
            Blue = new Texture2D(1, 1);
            Blue.SetPixel(0, 0, new Color(0, .578125f, .75f, 1));
            Blue.Apply();
            White = new Texture2D(1, 1);
            White.SetPixel(0, 0, new Color(1, 1, 1, .5f));
            White.Apply();
        }

        protected override void Draw(int num0) {
            Position.width = this.Width = AudioListener3D.ChannelCount * 30 + 30;
            TextAnchor OldAlign = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            Texture2D SecondOut = AudioListener3D.ChannelCount <= 4 ? Black : Orange;
            if (Peaks.Length != AudioListener3D.ChannelCount)
                Peaks = new float[AudioListener3D.ChannelCount];
            int Left = 25, Top = 25, Width = (int)Position.width - 4;
            int OldSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 12;
            GUI.Label(new Rect(0, Top, 30, 14), (DynamicRange / 10).ToString());
            for (int i = 2; i <= 10; i++) {
                GUI.Label(new Rect(0, Top += 14, 30, 14), (DynamicRange * i / 10).ToString());
                GUI.DrawTexture(new Rect(2, Top, Width, 1), White);
            }
            GUI.skin.label.fontSize = OldSize;
            for (int Channel = 0; Channel < AudioListener3D.ChannelCount; Channel++) {
                float Max = 0;
                int MultichannelUpdateRate = AudioListener3D.Output.Length;
                for (int Sample = Channel; Sample < MultichannelUpdateRate; Sample += AudioListener3D.ChannelCount)
                    if (Max < AudioListener3D.Output[Sample])
                        Max = AudioListener3D.Output[Sample];
                float CurrentBarHeight = SignalToDb(Max) / -DynamicRange + 1, CurrentPeak = Peaks[Channel] - Time.deltaTime;
                if (CurrentPeak < CurrentBarHeight)
                    CurrentPeak = CurrentBarHeight;
                if (CurrentPeak < 0)
                    CurrentPeak = 0;
                Peaks[Channel] = CurrentPeak;
                int Height = (int)(Peaks[Channel] * 140);
                GUI.DrawTexture(new Rect(Left += 5, 165 - Height, 20, Height), Channel < 2 ? Green : (Channel < 4 ? SecondOut : (Channel < 6 ? Black : (Channel < 8 ? Grey : Blue))));
                Left += 25;
            }
            GUI.skin.label.alignment = OldAlign;
            GUI.DragWindow();
        }
    }
}
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.Debug {
    /// <summary>
    /// Setup window for <see cref="SeatAdaptation"/>.
    /// </summary>
    [AddComponentMenu("Audio/Debug/Seat Adaptation Demo")]
    public class SeatAdaptationDemo : WindowBase {
        /// <summary>
        /// The Seat Adaptation component to configure.
        /// </summary>
        [Tooltip("The Seat Adaptation component to configure.")]
        public SeatAdaptation Adaptor;

        Texture2D Gray;

        /// <summary>
        /// Window dimension, name, and custom variable setup.
        /// </summary>
        protected override void Setup() {
            width = Adaptor.Columns * 20 + 5;
            height = Adaptor.Rows * 20 + 25;
            title = "Seat Adaptation";
            Gray = new Texture2D(1, 1);
            Gray.SetPixel(0, 0, new Color(0, 0, 0, .5f));
            Gray.Apply();
        }

        /// <summary>
        /// Draw window contents.
        /// </summary>
        /// <param name="ID">Window ID</param>
        protected override void Draw(int ID) {
            GUI.DrawTexture(new Rect(5, 20, Adaptor.Columns * 20 - 5, 2), Gray);
            for (int z = 0; z < Adaptor.Rows; ++z) {
                for (int x = 0; x < Adaptor.Columns; ++x) {
                    Adaptor.SeatsOccupied[z, x] =
                        GUI.Toggle(new Rect(x * 20 + 5, z * 20 + 22, 15, 15), Adaptor.SeatsOccupied[z, x], string.Empty);
                }
            }
            GUI.DragWindow();
        }
    }
}

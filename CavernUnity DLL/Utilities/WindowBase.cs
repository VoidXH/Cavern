using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Utilities {
    /// <summary>
    /// Cavern window handling basics.
    /// </summary>
    public abstract class WindowBase : MonoBehaviour {
        /// <summary>
        /// Possible corners to open a window at.
        /// </summary>
        public enum Corners {
            /// <summary>
            /// Top left corner.
            /// </summary>
            TopLeft,
            /// <summary>
            /// Top right corner.
            /// </summary>
            TopRight,
            /// <summary>
            /// Bottom left corner.
            /// </summary>
            BottomLeft,
            /// <summary>
            /// Borrom right corner.
            /// </summary>
            BottomRight
        };

        /// <summary>
        /// The corner to open the window at.
        /// </summary>
        [Tooltip("The corner to open the window at.")]
        public Corners Corner = Corners.TopLeft;

        /// <summary>
        /// Current window position.
        /// </summary>
        [System.NonSerialized] public Rect Position;

        /// <summary>
        /// GUI draw matrix override.
        /// </summary>
        [System.NonSerialized] public Matrix4x4 Matrix = Matrix4x4.identity;

        /// <summary>
        /// Window width.
        /// </summary>
        protected int width;

        /// <summary>
        /// Window height.
        /// </summary>
        protected int height;

        /// <summary>
        /// Window title.
        /// </summary>
        protected string title;

        /// <summary>
        /// Randomly generated window ID.
        /// </summary>
        int ID;

        /// <summary>
        /// Window dimension, name, and custom variable setup.
        /// </summary>
        protected abstract void Setup();

        /// <summary>
        /// Draw window contents.
        /// </summary>
        /// <param name="wID">Window ID</param>
        protected abstract void Draw(int wID);

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() {
            Setup();
            Position = new Rect(Corner == Corners.TopRight || Corner == Corners.BottomRight ? Screen.width : 0,
                                Corner == Corners.BottomLeft || Corner == Corners.BottomRight ? Screen.height : 0, width, height);
            ID = Random.Range(100000, int.MaxValue);
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnGUI() {
            GUI.matrix = Matrix;
            Position = GUI.Window(ID, Position, Draw, title);
            if (Position.x < 0) {
                Position.x = 0;
            }
            if (Position.y < 0) {
                Position.y = 0;
            }
            if (Position.x > Screen.width - Position.width) {
                Position.x = Screen.width - Position.width;
            }
            if (Position.y > Screen.height - Position.height) {
                Position.y = Screen.height - Position.height;
            }
        }
    }
}
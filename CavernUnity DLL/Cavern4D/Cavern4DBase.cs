using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Cavernize;
using Cavern.Channels;
using Cavern.Utilities;

namespace Cavern.Cavern4D {
    /// <summary>
    /// Seat movement generation for <see cref="Cavernize"/>.
    /// </summary>
    [AddComponentMenu("Audio/4D Processor")]
    public class Cavern4DBase : MonoBehaviour {
        /// <summary>
        /// The Cavernized audio object to be converted.
        /// </summary>
        [Tooltip("The Cavernized audio object to be converted.")]
        public Cavernizer CavernSource;

        /// <summary>
        /// Rotation aggressiveness.
        /// </summary>
        [Tooltip("Rotation aggressiveness.")]
        public float RotationConstant = 90;

        /// <summary>
        /// Maximum forward and backward seat rotation.
        /// </summary>
        [Tooltip("Maximum forward and backward seat rotation.")]
        [Range(0, 45)] public float MaxRotationFace = 20;

        /// <summary>
        /// Maximum sideways seat rotation.
        /// </summary>
        [Tooltip("Maximum sideways seat rotation.")]
        [Range(0, 45)] public float MaxRotationSide = 10;

        /// <summary>
        /// Number of seat rows.
        /// </summary>
        [Tooltip("Number of seat rows.")]
        public int Rows;

        /// <summary>
        /// Number of seats in a row.
        /// </summary>
        [Tooltip("Number of seats in a row.")]
        public int Columns;

        /// <summary>
        /// Seat movement description.
        /// </summary>
        public struct SeatData {
            /// <summary>
            /// Seat elevation in Cavernize's bounds.
            /// </summary>
            [Tooltip("Seat elevation in Cavernize's bounds.")]
            public float Height;

            /// <summary>
            /// Seat rotation Euler angles.
            /// </summary>
            public Vector3 Rotation;
        }

        /// <summary>
        /// Seat movement descriptions. The first dimension is the row, the second is the column.
        /// </summary>
        public SeatData[][] SeatMovements;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() {
            SeatMovements = new SeatData[Rows][];
            for (int row = 0; row < Rows; row++) {
                SeatMovements[row] = new SeatData[Columns];
            }
        }

        void OnDisable() {
            for (int row = 0; row < Rows; row++) {
                Array.Clear(SeatMovements[row], 0, Columns);
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            if (!CavernSource) {
                OnDisable();
                return;
            }
            for (int row = 0; row < Rows; row++) {
                for (int column = 0; column < Columns; column++) {
                    SeatMovements[row][column].Height = 200;
                }
            }
            int lastRow = Rows - 1, lastColumn = Columns - 1;
            if (CavernSource[0] != null) { // Front left
                SeatMovements[0][0].Height = CavernSource[0].Height;
            }
            if (CavernSource[1] != null) { // Front right
                SeatMovements[0][lastColumn].Height = CavernSource[1].Height;
            }
            if (CavernSource[2] != null && CavernSource[2].Height != Cavernizer.unsetHeight) { // Center
                SeatMovements[0][lastColumn / 2].Height = CavernSource[2].Height;
            }
            if (CavernSource[6] != null) { // Side left
                SeatMovements[lastRow][0].Height = CavernSource[6].Height;
            }
            if (CavernSource[7] != null) { // Side right
                SeatMovements[lastRow][lastColumn].Height = CavernSource[7].Height;
            }
            // Addition is okay, and should be used, as the rears are near the sides in the back corners.
            if (CavernSource[4] != null) { // Rear left
                SeatMovements[lastRow][0].Height += CavernSource[4].Height;
            }
            if (CavernSource[5] != null) { // Rear right
                SeatMovements[lastRow][lastColumn].Height += CavernSource[5].Height;
            }
            SpatializedChannel rearCenter = CavernSource.GetChannel(ReferenceChannel.RearCenter);
            if (rearCenter != null) { // Rear center
                SeatMovements[lastRow][lastColumn / 2].Height = rearCenter.Height;
            }

            // Use the front channels for moving all seats if nothing else is available for the rear sides
            if (SeatMovements[lastRow][0].Height == 200) {
                SeatMovements[lastRow][0].Height = SeatMovements[0][0].Height;
            }
            if (SeatMovements[lastRow][lastColumn].Height == 200) {
                SeatMovements[lastRow][lastColumn].Height = SeatMovements[0][lastColumn].Height;
            }

            // Seat position interpolation
            for (int row = 0; row < Rows; row++) {
                int prev = 0;
                for (int column = 0; column < Columns; column++) {
                    if (SeatMovements[row][column].Height != 200) {
                        float lerpDiv = column - prev;
                        for (int oldColumn = prev; oldColumn < column; oldColumn++) {
                            SeatMovements[row][oldColumn].Height = QMath.Lerp(SeatMovements[row][prev].Height,
                                SeatMovements[row][column].Height, (oldColumn - prev) / lerpDiv);
                        }
                        prev = column;
                    }
                }
                if (prev != lastColumn) {
                    float lerpDiv = lastColumn - prev;
                    for (int oldColumn = prev; oldColumn < lastColumn; oldColumn++) {
                        SeatMovements[row][oldColumn].Height = QMath.Lerp(SeatMovements[row][prev].Height,
                            SeatMovements[row][lastColumn].Height, (oldColumn - prev) / lerpDiv);
                    }
                }
            }
            for (int column = 0; column < Columns; column++) {
                int prev = 0;
                for (int row = 0; row < Rows; row++) {
                    if (SeatMovements[row][column].Height != 200) {
                        float lerpDiv = row - prev;
                        for (int oldRow = prev; oldRow < row; oldRow++) {
                            SeatMovements[oldRow][column].Height = QMath.Lerp(SeatMovements[prev][column].Height,
                                SeatMovements[row][column].Height, (oldRow - prev) / lerpDiv);
                        }
                        prev = row;
                    }
                }
                if (prev != lastRow) {
                    float lerpDiv = lastRow - prev;
                    for (int oldRow = prev; oldRow < lastRow; ++oldRow) {
                        SeatMovements[oldRow][column].Height = QMath.Lerp(SeatMovements[prev][column].Height,
                            SeatMovements[lastRow][column].Height, (oldRow - prev) / lerpDiv);
                    }
                }
            }

            // Seat rotation interpolation
            for (int row = 0; row < Rows; row++) {
                var movement = SeatMovements[row];
                movement[0].Rotation.z =
                    Mathf.Clamp((movement[1].Height - movement[0].Height) * RotationConstant * 2, -MaxRotationSide, MaxRotationSide);
                for (int column = 1; column < lastColumn; column++) {
                    movement[column].Rotation.z = Mathf.Clamp((movement[column + 1].Height - movement[column - 1].Height) *
                        RotationConstant, -MaxRotationSide, MaxRotationSide);
                }
                movement[lastColumn].Rotation.z = Mathf.Clamp((movement[lastColumn].Height - movement[lastColumn - 1].Height) *
                    RotationConstant * 2, -MaxRotationSide, MaxRotationSide);
            }
            for (int column = 0; column < Columns; column++) {
                SeatMovements[0][column].Rotation.x =
                    Mathf.Clamp((SeatMovements[1][column].Height - SeatMovements[0][column].Height) * RotationConstant * 2, -20, 20);
                for (int row = 1; row < lastRow; row++) {
                    SeatMovements[row][column].Rotation.x =
                        Mathf.Clamp((SeatMovements[row + 1][column].Height - SeatMovements[row - 1][column].Height) *
                        RotationConstant, -MaxRotationFace, MaxRotationFace);
                }
                SeatMovements[lastRow][column].Rotation.x =
                    Mathf.Clamp((SeatMovements[lastRow][column].Height - SeatMovements[lastRow - 1][column].Height) *
                    RotationConstant * 2, -MaxRotationFace, MaxRotationFace);
            }
        }
    }
}
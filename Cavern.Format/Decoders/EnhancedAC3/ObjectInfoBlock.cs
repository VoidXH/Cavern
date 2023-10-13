using System;
using System.Numerics;

using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class ObjectInfoBlock {
        /// <summary>
        /// This block contained a position information update.
        /// </summary>
        public bool ValidPosition { get; private set; }

        /// <summary>
        /// This object is not dynamic, but used as a bed channel.
        /// </summary>
        public bool IsBed => anchor == ObjectAnchor.Speaker;

        /// <summary>
        /// This frame contains the difference from the last object position instead of an exact position.
        /// </summary>
        bool differentialPosition;

        /// <summary>
        /// Object volume multiplier. Any negative value means reusing the last gain.
        /// </summary>
        float gain = -1;

        /// <summary>
        /// Object distance from the center of the room. Distant objects are mapped closer to the listening position.
        /// </summary>
        float distance;

        /// <summary>
        /// Object size. Any negative value means reusing the last size.
        /// </summary>
        float size = -1;

        /// <summary>
        /// A multiplier in screen-anchored rendering.
        /// </summary>
        float depthFactor, screenFactor;

        /// <summary>
        /// The object's anchor position.
        /// </summary>
        ObjectAnchor anchor = ObjectAnchor.Room;

        /// <summary>
        /// The coded position information, either exact or differential.
        /// </summary>
        Vector3 position;

        /// <summary>
        /// Last fully transmitted position to add delta positions to.
        /// </summary>
        Vector3 lastPrecise;

        /// <summary>
        /// Read new information for this block.
        /// </summary>
        public void Update(BitExtractor extractor, int blk, bool bedOrISFObject) {
            bool inactive = extractor.ReadBit();
            int basicInfoStatus = inactive ? 0 : (blk == 0 ? 1 : extractor.Read(2));
            if ((basicInfoStatus & 1) == 1) {
                ObjectBasicInfo(extractor, basicInfoStatus == 1);
            }

            int renderInfoStatus = 0;
            if (!inactive && !bedOrISFObject) {
                renderInfoStatus = blk == 0 ? 1 : extractor.Read(2);
            }
            if ((renderInfoStatus & 1) == 1) {
                ObjectRenderInfo(extractor, blk, renderInfoStatus == 1);
            }

            if (extractor.ReadBit()) { // Additional table data
                extractor.Skip((extractor.Read(4) + 1) * 8);
            }

            if (bedOrISFObject) {
                anchor = ObjectAnchor.Speaker;
            }
        }

        /// <summary>
        /// Sets the properties of the block, returns the final target position.
        /// The position shouldn't be updated immediately, it might have a ramp.
        /// </summary>
        public Vector3 UpdateSource(Source source) {
            if (gain >= 0) {
                source.Volume = gain;
            }
            if (size >= 0) {
                source.Size = size;
            }

            if (ValidPosition && anchor != ObjectAnchor.Speaker) {
                if (differentialPosition) {
                    position = new Vector3(
                        Math.Clamp(lastPrecise.X + position.X, 0, 1),
                        Math.Clamp(lastPrecise.Y + position.Y, 0, 1),
                        Math.Clamp(lastPrecise.Z + position.Z, 0, 1)
                    );
                } else {
                    lastPrecise = position;
                }

                switch (anchor) {
                    case ObjectAnchor.Room:
                        if (!float.IsNaN(distance)) {
                            Vector3 intersect = position.MapToCube();
                            float distanceFactor = intersect.Length() / distance;
                            position = distanceFactor * intersect + (1 - distanceFactor) * roomCenter;
                        }
                        source.screenLocked = false;
                        break;
                    case ObjectAnchor.Screen:
                        Vector3 reference =
                            new Vector3((position.X - .5f) * Listener.ScreenSize.X + .5f,
                            position.Y,
                            (position.Z + 1) * Listener.ScreenSize.Y
                        );
                        Vector3 screenFactorMultiplier = new Vector3(screenFactor, 1, screenFactor);
                        float depth = MathF.Pow(position.Y, depthFactor);
                        Vector3 depthFactorMultiplier = new Vector3(depth, 1, depth);
                        position = depthFactorMultiplier * (screenFactorMultiplier * position + reference -
                            screenFactorMultiplier * reference) + reference - depthFactorMultiplier * reference;
                        source.screenLocked = true;
                        break;
                }
            }

            // Convert to Cavern coordinate space
            return Listener.EnvironmentSize * new Vector3(
                position.X * 2 - 1,
                position.Z,
                position.Y * -2 + 1
            );
        }

        void ObjectBasicInfo(BitExtractor extractor, bool readAllBlocks) {
            int blocks = readAllBlocks ? 3 : extractor.Read(2);

            // Gain
            if ((blocks & 2) != 0) {
                int gainHelper = extractor.Read(2);
                gain = gainHelper switch {
                    0 => 1,
                    1 => 0,
                    2 => QMath.DbToGain((gainHelper = extractor.Read(6)) < 15 ? 15 - gainHelper : 14 - gainHelper),
                    _ => -1,
                } * .707f; // 3 dB attenuation as some content clip without this
            }

            // Priority - unneccessary, everything's rendered
            if ((blocks & 1) != 0 && !extractor.ReadBit()) {
                extractor.Skip(5);
            }
        }

        void ObjectRenderInfo(BitExtractor extractor, int blk, bool readAllBlocks) {
            int blocks = readAllBlocks ? 15 : extractor.Read(4);

            // Spatial position
            if (ValidPosition = (blocks & 1) != 0) {
                differentialPosition = blk != 0 && extractor.ReadBit();
                if (differentialPosition) {
                    position = new Vector3(
                        extractor.ReadSigned(3) * xyScale,
                        extractor.ReadSigned(3) * xyScale,
                        extractor.ReadSigned(3) * zScale
                    );
                } else {
                    int posX = extractor.Read(6);
                    int posY = extractor.Read(6);
                    int posZ = ((extractor.ReadBitInt() << 1) - 1) * extractor.Read(4);
                    position = new Vector3(
                        Math.Min(1, posX * xyScale),
                        Math.Min(1, posY * xyScale),
                        Math.Min(1, posZ * zScale)
                    );
                }
                if (extractor.ReadBit()) { // Distance specified
                    if (extractor.ReadBit()) { // Infinite distance
                        distance = 100; // Close enough
                    } else {
                        distance = distanceFactors[extractor.Read(4)];
                    }
                } else {
                    distance = float.NaN;
                }
            }

            // Zone constraints - the renderer is not prepared for zoning
            if ((blocks & 2) != 0) {
                extractor.Skip(4);
            }

            // Scaling
            if ((blocks & 4) != 0) {
                size = extractor.Read(2) switch {
                    0 => 0,
                    1 => extractor.Read(5) * sizeScale,
                    2 => (new Vector3(extractor.Read(5), extractor.Read(5), extractor.Read(5)) * sizeScale).Length(),
                    _ => -1,
                };
            }

            // Screen anchoring
            if ((blocks & 8) != 0 && extractor.ReadBit()) {
                anchor = ObjectAnchor.Screen;
                screenFactor = (extractor.Read(3) + 1) * .125f;
                depthFactor = depthFactors[extractor.Read(2)];
            }

            extractor.Skip(1); // Snap to the nearest channel - unused
        }

        const float xyScale = 1 / 62f;
        const float zScale = 1 / 15f;
        const float sizeScale = 1 / 31f;
        static readonly Vector3 roomCenter = new Vector3(.5f, .5f, 0);
        static readonly float[] distanceFactors =
            { 1.1f, 1.3f, 1.6f, 2.0f, 2.5f, 3.2f, 4.0f, 5.0f, 6.3f, 7.9f, 10.0f, 12.6f, 15.8f, 20.0f, 25.1f, 50.1f };
        static readonly float[] depthFactors = { .25f, .5f, 1, 2 };
    }
}
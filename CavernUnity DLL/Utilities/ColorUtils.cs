using UnityEngine;

namespace Cavern.Utilities {
    /// <summary>
    /// Useful color functions used in multiple classes.
    /// </summary>
    public static class ColorUtils {
        /// <summary>
        /// Cavern's symbolic blue.</summary>
        public static readonly Color CavernBlue = new Color(0, .578125f, .75f, 1);

        /// <summary>
        /// Standard color of the front (green) jack.
        /// </summary>
        public static readonly Color frontJack = new Color(.596078431f, .984313725f, .596078431f, 1);

        /// <summary>
        /// Standard color of the center (orange) jack.
        /// </summary>
        public static readonly Color centerJack = new Color(1, .647058824f, 0, 1);

        /// <summary>
        /// Standard color of the side (gray) jack.
        /// </summary>
        public static readonly Color sideJack = new Color(.75294117647f, .75294117647f, .75294117647f, 1);

        /// <summary>
        /// Get the color of a channel that should be used when displaying a massively multichannel system.
        /// </summary>
        public static Color GetChannelColor(int channel) {
            System.Numerics.Vector3 channelPos = Listener.Channels[channel].CubicalPos;
            if (Listener.Channels[channel].LFE) {
                return Color.black;
            }
            float hue;
            if (channelPos.Z % 1 == 0) {
                hue = (channelPos.Y + 1f) * channelPos.Z * 45f + 180f;
            } else {
                hue = channelPos.X * (channelPos.Y + 1f) * 22.5f + 45f;
            }
            Color targetColor = GetHueColor(hue);
            return new Color(targetColor.r * .75f + .25f, targetColor.g * .75f + .25f, targetColor.b * .75f + .25f);
        }

        /// <summary>
        /// Get the Cavern or Jack port color of a channel.
        /// </summary>
        public static Color GetChannelColor(int channel, bool jackColoring) {
            if (jackColoring) {
                return GetJackColor(channel);
            }
            return GetChannelColor(channel);
        }

        /// <summary>
        /// Get a color by hue value.
        /// </summary>
        /// <param name="degrees">Hue value in degrees.</param>
        public static Color GetHueColor(float degrees) {
            degrees %= 360;
            if (degrees < 0) {
                degrees += 360f;
            }
            if (degrees < 120) {
                if (degrees < 60) {
                    return new Color(1f, degrees / 60f, 0f);
                }
                return new Color(1f - (degrees - 60f) / 60f, 1f, 0f);
            } else if (degrees < 240) {
                if (degrees < 180) {
                    return new Color(0, 1f, (degrees - 120f) / 60f);
                }
                return new Color(0, 1f - (degrees - 180f) / 60f, 1f);
            } else {
                if (degrees < 300) {
                    return new Color((degrees - 240f) / 60f, 0f, 1f);
                }
                return new Color(1f, 0f, 1f - (degrees - 300f) / 60f);
            }
        }

        /// <summary>
        /// Get a Jack color associated to a standard output channel by the layout currently set.
        /// <see cref="CavernBlue"/> is returned when the channel is invalid for the 8-channel Jack out standard.
        /// </summary>
        public static Color GetJackColor(int channel) {
            switch (channel) {
                case 0:
                case 1:
                    return frontJack;
                case 2:
                case 3:
                    return Listener.Channels.Length <= 4 ? Color.black : centerJack;
                case 4:
                case 5:
                    return Color.black;
                case 6:
                case 7:
                    return sideJack;
                default:
                    return CavernBlue;
            }
        }

        /// <summary>
        /// Convert a Unity <see cref="Color"/> to ARGB which Cavern's graphing is using.
        /// </summary>
        public static uint ToARGB(this Color color) =>
            ((uint)(color.a * 255) << 24) | (uint)(color.r * 255) << 16 | (uint)(color.g * 255) << 8 | (uint)(color.b * 255);
    }
}
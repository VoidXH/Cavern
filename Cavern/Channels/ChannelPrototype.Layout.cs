namespace Cavern.Channels {
    partial struct ChannelPrototype {
        /// <summary>
        /// Convert a prototype array to a <see cref="Channel"/> array that can be set in <see cref="Listener.Channels"/>.
        /// </summary>
        public static Channel[] ToLayout(ChannelPrototype[] source) {
            Channel[] result = new Channel[source.Length];
            for (int channel = 0; channel < source.Length; ++channel) {
                result[channel] = new Channel(source[channel].X, source[channel].Y, source[channel].LFE);
            }
            return result;
        }

        /// <summary>
        /// Convert a reference array to a <see cref="Channel"/> array that can be set in <see cref="Listener.Channels"/>.
        /// </summary>
        public static Channel[] ToLayout(ReferenceChannel[] source) => ToLayout(Get(source));

        /// <summary>
        /// Convert a reference array to a <see cref="Channel"/> array that can be set in <see cref="Listener.Channels"/>.
        /// </summary>
        /// <param name="source">The reference channels to convert.</param>
        /// <param name="alternative">Whether to use alternative (encoded immpersive) positions.</param>
        public static Channel[] ToLayout(ReferenceChannel[] source, bool alternative) => alternative ? ToLayoutAlternative(source) : ToLayout(source);

        /// <summary>
        /// Convert a reference array to a <see cref="Channel"/> array that can be set in <see cref="Listener.Channels"/>,
        /// using the <see cref="AlternativePositions"/>.
        /// </summary>
        public static Channel[] ToLayoutAlternative(ReferenceChannel[] source) {
            ChannelPrototype[] prototypes = GetAlternative(source);
            Channel[] result = new Channel[source.Length];
            for (int i = 0; i < source.Length; i++) {
                result[i] = new Channel(prototypes[i].X, prototypes[i].Y, source[i] == ReferenceChannel.ScreenLFE);
            }
            return result;
        }
    }
}

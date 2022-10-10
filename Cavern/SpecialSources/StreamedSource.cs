namespace Cavern.SpecialSources {
    /// <summary>
    /// An always rendered source where only <see cref="Source.GetSamples"/> or <see cref="Source.Collect"/> should be overridden.
    /// </summary>
    public class StreamedSource : Source {
        /// <summary>
        /// Force the source to be played.
        /// </summary>
        protected internal override bool Precollect() {
            ForcePrecollect();
            return base.Precollect();
        }

        /// <summary>
        /// Indicates that the source meets rendering requirements, and <see cref="Source.GetSamples"/> won't fail.
        /// </summary>
        protected internal override bool Renderable => IsPlaying;
    }
}
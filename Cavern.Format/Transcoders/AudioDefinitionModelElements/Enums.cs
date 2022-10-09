namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Supported dialogue mixing methods.
    /// </summary>
    public enum ADMDialogueType {
        /// <summary>
        /// One of the mixing methods in <see cref="NonDialogueContentKind"/>.
        /// </summary>
        nonDialogueContentKind,
        /// <summary>
        /// One of the mixing methods in <see cref="DialogueContentKind"/>.
        /// </summary>
        dialogueContentKind,
        /// <summary>
        /// One of the mixing methods in <see cref="MixedContentKind"/>.
        /// </summary>
        mixedContentKind
    }

    /// <summary>
    /// Supported values for the <see cref="ADMDialogueType.nonDialogueContentKind"/> dialogue mixing method.
    /// </summary>
    public enum NonDialogueContentKind {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Undefined,
        Music,
        Effect
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// Supported values for the <see cref="ADMDialogueType.dialogueContentKind"/> dialogue mixing method.
    /// </summary>
    public enum DialogueContentKind {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Undefined,
        StorylineDialogue,
        Voiceover,
        SpokenSubtitle,
        VisuallyImpaired,
        Commentary,
        Emergency
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// Supported values for the <see cref="ADMDialogueType.mixedContentKind"/> dialogue mixing method.
    /// </summary>
    public enum MixedContentKind {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Undefined,
        CompleteMain,
        Mixed,
        HearingImpaired
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// Supported <see cref="ADMPackFormat"/> types.
    /// </summary>
    public enum ADMPackType {
        /// <summary>
        /// For channel-based audio, where each channel feeds a speaker directly.
        /// </summary>
        DirectSpeakers = 1,
        /// <summary>
        /// For channel-based audio where channels are matrixed together, such as Mid-Side, Lt/Rt.
        /// </summary>
        Matrix,
        /// <summary>
        /// For object-based audio where channels represent audio objects and position updates are provided.
        /// </summary>
        Objects,
        /// <summary>
        /// For scene-based audio where Ambisonics and HOA are used.
        /// </summary>
        HOA,
        /// <summary>
        /// For binaural audio, where playback is over headphones.
        /// </summary>
        Binaural
    }

    /// <summary>
    /// Known encodings of ADM tracks.
    /// </summary>
    public enum ADMTrackCodec {
        /// <summary>
        /// Pulse code modulation.
        /// </summary>
        PCM = 1
    }
}
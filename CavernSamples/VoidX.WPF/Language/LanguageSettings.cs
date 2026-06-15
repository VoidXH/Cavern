namespace VoidX.WPF.Language {
    /// <summary>
    /// Global settings related to <see cref="LanguageBase{T}"/> inheritors.
    /// </summary>
    public static class LanguageSettings {
        /// <summary>
        /// Load this culture (if present) instead of the user's culture.
        /// </summary>
        public static string CultureOverride { get; set; }
    }
}

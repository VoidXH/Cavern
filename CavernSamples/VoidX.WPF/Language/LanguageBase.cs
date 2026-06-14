using System.Collections.Generic;
using System.Globalization;

namespace VoidX.WPF.Language {
    /// <summary>
    /// Stores translations of a set of language strings. The base (English) translation shall be a derived item of this class, filling the <see cref="language"/>
    /// dictionary with the constructor of this class. <typeparamref name="T"/> shall be the derived class itself, it makes the <see cref="Active"/> instance accessible.
    /// Translations shall derive the base (English) translation and overwrite it by calling <see cref="Set(string, string)"/> from an overridden
    /// <see cref="ApplyTranslation"/>. That function will be called if the translation's <see cref="CultureCode"/> matches the user's culture.
    /// </summary>
    /// <remarks>Always use through <see cref="Active"/>, otherwise the root language is used.</remarks>
    public abstract class LanguageBase<T> where T : LanguageBase<T>, new() {
        /// <summary>
        /// Get a translation by <paramref name="key"/>.
        /// </summary>
        public string this[string key] => language[key];

        /// <summary>
        /// Language of the translation.
        /// </summary>
        protected virtual string CultureCode => "en-US";

        /// <summary>
        /// The translation in the user's language.
        /// </summary>
        public static T Active {
            get {
                if (active == null) {
                    active = new T();
                    active.Initialize();
                }
                return active;
            }
        }
        static T active;

        /// <summary>
        /// Translated strings to the user's language.
        /// </summary>
        Dictionary<string, string> language;

        /// <summary>
        /// Set up the localization environment for this set of strings.
        /// </summary>
        protected LanguageBase(Dictionary<string, string> language) => this.language = language;

        /// <summary>
        /// Return one instance for all available translations.
        /// </summary>
        protected abstract LanguageBase<T>[] GetTranslations();

        /// <summary>
        /// Overwrite the base translation using <see cref="Set(string, string)"/> calls.
        /// </summary>
        protected virtual void ApplyTranslation() { }

        /// <summary>
        /// Translate a language string by <paramref name="key"/>. Only existing keys can be translated.
        /// </summary>
        protected void Set(string key, string value) {
            if (language.ContainsKey(key)) {
                language[key] = value;
            } else {
                throw new KeyNotFoundException();
            }
        }

        /// <summary>
        /// Determines the target culture and requests the derived class to provide and apply matching translations.
        /// </summary>
        void Initialize() {
            string targetCulture = CultureInfo.CurrentUICulture.Name;
            if (!string.IsNullOrEmpty(LanguageSettings.CultureOverride)) {
                targetCulture = LanguageSettings.CultureOverride;
            }

            LanguageBase<T>[] translations = GetTranslations();
            for (int i = 0; i < translations.Length; i++) {
                if (translations[i].CultureCode == targetCulture) {
                    translations[i].ApplyTranslation();
                    language = translations[i].language;
                    return;
                }
            }
        }
    }
}

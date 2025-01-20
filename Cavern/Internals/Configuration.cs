using System;
using System.Globalization;
using System.IO;

namespace Cavern.Internals {
    /// <summary>
    /// Functions to modify Cavern's global configuration. See <see cref="IKnowWhatIAmDoing"/> for warnings.
    /// </summary>
    public static class CavernConfiguration {
        /// <summary>
        /// These are very wild waters and you should really evaluate if you need these functions or not.
        /// You are given the power to modify the user's global settings which are reflected in all Cavern
        /// products and applications/games built on Cavern. If the user is not properly prompted that for
        /// example, the speakers are getting reordered in all applications, they can blame Cavern for bugs
        /// it doesn't have. Your page names, prompts, and UIs should completely convey that the user is
        /// editing the GLOBAL settings. You should really make it sure they WANT to do EXACTLY what is
        /// being called from here. To be safe and always support the Cavern ecosystem properly, just ask
        /// the user to use the Cavern Driver. If they never used Cavern Driver, Cavern falls back to 5.1,
        /// which is the only safe option to mix to any channel layout with limited system knowledge.
        /// If you're extra sure you won't break the user's setup, set this to true to use the class.
        /// </summary>
        public static bool IKnowWhatIAmDoing { get; set; }

        /// <summary>
        /// Get the path of the folder that contains Cavern's configuration files.
        /// </summary>
        public static string GetPath() {
            if (!IKnowWhatIAmDoing) {
                throw new DevHasNoIdeaException();
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cavern");
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        /// <summary>
        /// Overwrite the global environment (including channel layout) used by all applications built on Cavern.
        /// </summary>
        public static void SaveCurrentLayoutAsDefault() => SaveCurrentLayoutAs("Save");

        /// <summary>
        /// Save an environment (including channel layout) preset option that can be recalled in the Cavern Driver.
        /// </summary>
        public static void SaveCurrentLayoutAsPreset(string presetName) => SaveCurrentLayoutAs(presetPrefix + presetName);

        /// <summary>
        /// Delete a preset that was saved with <see cref="SaveCurrentLayoutAsPreset(string)"/>.
        /// </summary>
        public static void DeletePreset(string presetName) => File.Delete(Path.Combine(GetPath(), $"{presetPrefix}{presetName}.dat"));

        /// <summary>
        /// Save an environment (including channel layout) preset option that can be recalled in the Cavern Driver.
        /// </summary>
        static void SaveCurrentLayoutAs(string presetName) {
            Channel[] channels = Listener.Channels;
            string[] save = new string[channels.Length * 3 + 7];
            save[0] = channels.Length.ToString();
            int savePos = 1;
            for (int i = 0; i < channels.Length; i++) {
                save[savePos] = channels[i].X.ToString(CultureInfo.InvariantCulture);
                save[savePos + 1] = channels[i].Y.ToString(CultureInfo.InvariantCulture);
                save[savePos + 2] = channels[i].LFE.ToString();
                savePos += 3;
            }
            save[savePos] = ((int)Listener.EnvironmentType).ToString();
            save[savePos + 1] = Listener.EnvironmentSize.X.ToString(CultureInfo.InvariantCulture);
            save[savePos + 2] = Listener.EnvironmentSize.Y.ToString(CultureInfo.InvariantCulture);
            save[savePos + 3] = Listener.EnvironmentSize.Z.ToString(CultureInfo.InvariantCulture);
            save[savePos + 4] = Listener.HeadphoneVirtualizer.ToString();
            save[savePos + 5] = string.Empty; // Was: environment compensation
            File.WriteAllLines(Path.Combine(GetPath(), presetName + ".dat"), save);
        }

        /// <summary>
        /// The name of all environment preset files in Cavern's configuration folder start with this.
        /// </summary>
        const string presetPrefix = "CavernPreset_";
    }
}
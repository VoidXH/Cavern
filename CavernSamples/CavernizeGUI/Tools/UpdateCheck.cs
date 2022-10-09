﻿using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace VoidX.WPF {
    /// <summary>
    /// Checks for application updates in a separate thread once a week.
    /// </summary>
    public class UpdateCheck {
        /// <summary>
        /// Run the update check in a separate thread.
        /// </summary>
        /// <param name="lastCheck">Last time an update check was performed</param>
        /// <param name="onChecked">When the check was performed, call this function - used to keep track of the last check</param>
        public static void Perform(DateTime lastCheck, Action onChecked) => Task.Run(() => CheckForUpdate(lastCheck, onChecked));

        /// <summary>
        /// The actual work that is performed in the update thread.
        /// </summary>
        /// <param name="lastCheck">Last time an update check was performed</param>
        /// <param name="onChecked">When the check was performed, call this function - used to keep track of the last check</param>
        static async void CheckForUpdate(DateTime lastCheck, Action onChecked) {
            if (DateTime.Now < lastCheck + TimeSpan.FromDays(7)) {
                return;
            }

            HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(updateLocation);
            response.EnsureSuccessStatusCode();
            string body = await response.Content.ReadAsStringAsync();

            if (thisRevision < int.Parse(body)) {
                if (MessageBox.Show("A new version is available! Do you want to download it?",
                    "Update available", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
                    Process.Start(new ProcessStartInfo() {
                        FileName = downloadLink,
                        UseShellExecute = true
                    });
                }
            }

            onChecked();
        }

        /// <summary>
        /// The HTTP query that results in getting the latest revision number.
        /// </summary>
        const string updateLocation = "http://sbence.hu/ver/cavg.php";

        /// <summary>
        /// The current revision number. If the received revision is newer, an &quot;update available&quot; message is shown.
        /// </summary>
        const int thisRevision = 1;

        /// <summary>
        /// The page where the new version can be downloaded.
        /// </summary>
        const string downloadLink = "http://cavern.sbence.hu/cavern/downloads.php";
    }
}
using System.Runtime.CompilerServices;

namespace VoidX.WPF {
    /// <summary>
    /// HTTP utilities.
    /// </summary>
    public static class HTTP {
        /// <summary>
        /// Gets a HTTP resource with a 5 second timeout.
        /// </summary>
        public static string GET(string url) => GET(url, 5);

        /// <summary>
        /// Gets a HTTP resource with a timeout.
        /// </summary>
        public static string GET(string url, int timeoutSeconds) {
            HttpClient client = new HttpClient {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            try {
                HttpResponseMessage response = client.GetAsync(url).Result;
                if (response.IsSuccessStatusCode) {
                    return response.Content.ReadAsStringAsync().Result;
                }
            } catch {
                return null;
            }
            return null;
        }

        /// <summary>
        /// Sends a POST request of key-value pairs with a 5 second timeout.
        /// </summary>
        public static string POST(string url, KeyValuePair<string, string>[] data) => POST(url, data, 5);

        /// <summary>
        /// Sends a POST request of key-value pairs with a timeout.
        /// </summary>
        public static string POST(string url, KeyValuePair<string, string>[] data, int timeoutSeconds) {
            using FormUrlEncodedContent content = new FormUrlEncodedContent(data);
            return POST(url, content, timeoutSeconds);
        }

        /// <summary>
        /// Sends a POST request of large binary data with a 5 second timeout.
        /// </summary>
        public static string POST(string url, (string key, byte[] value)[] data) => POST(url, data, 5);

        /// <summary>
        /// Sends a POST request of large binary data with a timeout.
        /// </summary>
        public static string POST(string url, (string key, byte[] value)[] data, int timeoutSeconds) {
            using MultipartFormDataContent form = new MultipartFormDataContent();
            for (int i = 0; i < data.Length; i++) {
                form.Add(new ByteArrayContent(data[i].value), data[i].key);
            }
            return POST(url, form, timeoutSeconds);
        }

        /// <summary>
        /// Sends an arbitrary POST request with a 5 second timeout.
        /// </summary>
        public static string POST(string url, HttpContent content) => POST(url, content, 5);

        /// <summary>
        /// Sends an arbitrary POST request.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string POST(string url, HttpContent content, int timeoutSeconds) {
            using HttpClient client = new HttpClient {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = content
            };
            try {
                HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode) {
                    return response.Content.ReadAsStringAsync().Result;
                }
            } catch {
                return null;
            }
            return null;
        }
    }
}

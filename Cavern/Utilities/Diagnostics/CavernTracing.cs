namespace Cavern.Utilities.Diagnostics {
    /// <summary>
    /// Subscribe to messages about how Cavern operates under the hood.
    /// </summary>
    public static class CavernTracing {
        /// <summary>
        /// A lo <paramref name="level"/>-<paramref name="message"/> pair's handler.
        /// </summary>
        public delegate void CavernTraceCallback(CavernTraceLevel level, string message);

        /// <summary>
        /// Receive messages published through Cavern's tracing system.
        /// </summary>
        public static event CavernTraceCallback OnTrace;

        /// <summary>
        /// Broadcast a trace <paramref name="message"/> with a given logging <paramref name="level"/>.
        /// </summary>
        public static void Log(CavernTraceLevel level, string message) => OnTrace?.Invoke(level, message);

        /// <summary>
        /// Broadcast a formatted trace <paramref name="message"/> with a given logging <paramref name="level"/>.
        /// Optimized for assembling the message only when tracing is actually enabled to support maximum performance when tracing is disabled.
        /// </summary>
        public static void Log(CavernTraceLevel level, string message, object param1) => OnTrace?.Invoke(level, string.Format(message, param1));

        /// <summary>
        /// Broadcast a formatted trace <paramref name="message"/> with a given logging <paramref name="level"/>.
        /// Optimized for assembling the message only when tracing is actually enabled to support maximum performance when tracing is disabled.
        /// </summary>
        public static void Log(CavernTraceLevel level, string message, object param1, object param2) => OnTrace?.Invoke(level, string.Format(message, param1, param2));

        /// <summary>
        /// Broadcast a formatted trace <paramref name="message"/> with a given logging <paramref name="level"/>.
        /// Optimized for assembling the message only when tracing is actually enabled to support maximum performance when tracing is disabled.
        /// </summary>
        public static void Log(CavernTraceLevel level, string message, params object[] parameters) => OnTrace?.Invoke(level, string.Format(message, parameters));

        /// <summary>
        /// Broadcast a trace <paramref name="message"/> with a given logging <paramref name="level"/>.
        /// </summary>
        public static void Log(CavernTraceLevel level, object sender, string message) => OnTrace?.Invoke(level, $"[{sender.GetType().Name}] {message}");

        /// <summary>
        /// Broadcast a formatted trace <paramref name="message"/> with a given logging <paramref name="level"/>.
        /// Optimized for assembling the message only when tracing is actually enabled to support maximum performance when tracing is disabled.
        /// </summary>
        public static void Log(CavernTraceLevel level, object sender, string message, object param1) =>
            OnTrace?.Invoke(level, $"[{sender.GetType().Name}] {string.Format(message, param1)}");

        /// <summary>
        /// Broadcast a formatted trace <paramref name="message"/> with a given logging <paramref name="level"/>.
        /// Optimized for assembling the message only when tracing is actually enabled to support maximum performance when tracing is disabled.
        /// </summary>
        public static void Log(CavernTraceLevel level, object sender, string message, object param1, object param2) =>
            OnTrace?.Invoke(level, $"[{sender.GetType().Name}] {string.Format(message, param1, param2)}");

        /// <summary>
        /// Broadcast a formatted trace <paramref name="message"/> with a given logging <paramref name="level"/>.
        /// Optimized for assembling the message only when tracing is actually enabled to support maximum performance when tracing is disabled.
        /// </summary>
        public static void Log(CavernTraceLevel level, object sender, string message, params object[] parameters) =>
            OnTrace?.Invoke(level, $"[{sender.GetType().Name}] {string.Format(message, parameters)}");
    }
}

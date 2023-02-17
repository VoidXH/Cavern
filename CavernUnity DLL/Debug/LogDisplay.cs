using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.Debug {
    /// <summary>
    /// Displays the last logged message.
    /// </summary>
    [AddComponentMenu("Audio/Debug/Log Display")]
    public class LogDisplay : WindowBase {
        /// <summary>
        /// Maximum level to be reported.
        /// </summary>
        [Tooltip("Maximum level to be reported.")]
        public LogType LogLevel = LogType.Error;

        /// <summary>
        /// Last received log message that matches the criteria.
        /// </summary>
        string lastLog = "No message so far.";
        LogType lastType = LogType.Log;

        /// <summary>
        /// Window dimension, name, and custom variable setup.
        /// </summary>
        protected override void Setup() {
            width = 400;
            height = 120;
            title = "Log Display";
        }

        void LogHandler(string message, string stackTrace, LogType messageLevel) {
            if (messageLevel <= LogLevel) {
                lastLog = message + "\n\n" + stackTrace;
                lastType = messageLevel;
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnEnable() => Application.logMessageReceived += LogHandler;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() => Application.logMessageReceived -= LogHandler;

        /// <summary>
        /// Draw window contents.
        /// </summary>
        /// <param name="wID">Window ID</param>
        protected override void Draw(int wID) {
            Color oldColor = GUI.color;
            if (lastType <= LogType.Error) {
                GUI.color = Color.red;
            }
            TextAnchor oldAlign = GUI.skin.label.alignment;
            int oldSize = GUI.skin.label.fontSize;
            bool oldWrap = GUI.skin.label.wordWrap;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.wordWrap = true;
            GUI.Label(new Rect(0, 0, width, height), lastLog);
            GUI.color = oldColor;
            GUI.skin.label.alignment = oldAlign;
            GUI.skin.label.fontSize = oldSize;
            GUI.skin.label.wordWrap = oldWrap;
            GUI.DragWindow();
        }
    }
}

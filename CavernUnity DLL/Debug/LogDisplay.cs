using Cavern.Utilities;
using System;
using UnityEngine;

namespace Cavern.Debug {
    /// <summary>Displays the last logged message.</summary>
    public class LogDisplay : WindowBase {
        /// <summary>Maximum level to be reported.</summary>
        public LogType LogLevel = LogType.Error;

        /// <summary>Last received log message that matches the criteria.</summary>
        string LastLog = "No message so far.";
        LogType LastType = LogType.Log;

        /// <summary>Window dimension, name, and custom variable setup.</summary>
        protected override void Setup() {
            Width = 400;
            Height = 120;
            Title = "Log Display";
        }

        void LogHandler(string Message, string StackTrace, LogType MessageLevel) {
            if (MessageLevel <= LogLevel) {
                LastLog = Message + "\n\n" + StackTrace;
                LastType = MessageLevel;
            }
        }

        void OnEnable() {
            Application.logMessageReceived += LogHandler;
        }

        void OnDisable() {
            Application.logMessageReceived -= LogHandler;
        }

        /// <summary>Draw window contents.</summary>
        /// <param name="wID">Window ID</param>
        protected override void Draw(int wID) {
            Color OldColor = GUI.color;
            if (LastType <= LogType.Error)
                GUI.color = Color.red;
            TextAnchor OldAlign = GUI.skin.label.alignment;
            int OldSize = GUI.skin.label.fontSize;
            bool OldWrap = GUI.skin.label.wordWrap;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.wordWrap = true;
            GUI.Label(new Rect(0, 0, Width, Height), LastLog);
            GUI.color = OldColor;
            GUI.skin.label.alignment = OldAlign;
            GUI.skin.label.fontSize = OldSize;
            GUI.skin.label.wordWrap = OldWrap;
        }
    }
}

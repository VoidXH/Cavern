﻿using Microsoft.Msagl.Drawing;

using Cavern.Filters.Utilities;

namespace FilterStudio.Graphs {
    /// <summary>
    /// An MSAGL <see cref="Node"/> with display properties aligned for this application.
    /// </summary>
    public class StyledNode : Node {
        /// <summary>
        /// Border and text color.
        /// </summary>
        public Color Foreground {
            get {
                return Attr.Color;
            }
            set {
                Attr.Color = value;
                Label.FontColor = value;
            }
        }

        /// <summary>
        /// If this node represents a filter and not a label, this property is set to that specific node.
        /// </summary>
        public FilterGraphNode Filter { get; set; }

        /// <summary>
        /// An MSAGL <see cref="Node"/> with display properties aligned for this application.
        /// </summary>
        /// <param name="id">Referenced unique node name when connecting edges</param>
        /// <param name="label">Displayed node name</param>
        public StyledNode(string id, string label) : this(id, label, Color.White) { }

        /// <summary>
        /// An MSAGL <see cref="Node"/> with display properties aligned for this application.
        /// </summary>
        /// <param name="id">Referenced unique node name when connecting edges</param>
        /// <param name="label">Displayed node name</param>
        /// <param name="foreground">Border and text color</param>
        public StyledNode(string id, string label, Color foreground) : base(id) {
            LabelText = label;
            Foreground = foreground;
        }
    }
}
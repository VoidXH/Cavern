using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.WpfGraphControl;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace FilterStudio.Graphs {
    /// <summary>
    /// A <see cref="Graph"/>-displaying control with a wide range of manipulation features for the user.
    /// </summary>
    public class ManipulatableGraph : ScrollViewer {
        /// <summary>
        /// Called when the user left-clicks anywhere in the graph control bounds, passes the clicked edge or node.
        /// If the click doesn't hit any graph element, the <see cref="object"/> will be null.
        /// </summary>
        /// <remarks>The clicked node is not necessarily selected. To check that, use <see cref="SelectedNode"/>.</remarks>
        public event Action<object> OnLeftClick;

        /// <summary>
        /// Called when the user right-clicks anywhere in the graph control bounds, passes the clicked edge or node.
        /// If the click doesn't hit any graph element, the <see cref="object"/> will be null.
        /// </summary>
        public event Action<object> OnRightClick;

        /// <summary>
        /// The currently selected node is determined by border line thickness.
        /// </summary>
        public StyledNode SelectedNode => (StyledNode)viewer.Graph?.Nodes.FirstOrDefault(x => x.Attr.LineWidth > 1);

        /// <summary>
        /// Handle to MSAGL.
        /// </summary>
        /// <remarks>Setting to null doesn't clear the last displayed graph.</remarks>
        public Graph Graph {
            get => viewer.Graph;
            set {
                viewer.Graph = value;
                OnLeftClick?.Invoke(null); // Says nothing is selected now, nothing has a thick border on redraw
            }
        }

        /// <summary>
        /// Handles displaying and manipulating the graph.
        /// </summary>
        readonly GraphViewer viewer;

        /// <summary>
        /// A <see cref="Graph"/>-displaying control with a wide range of manipulation features for the user.
        /// </summary>
        public ManipulatableGraph() {
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            // An inner panel that acts as a window for the graph. The ScrollViewer is the curtain,
            // it keeps the graph in the bounds of the control wherever the user moves it.
            DockPanel panel = new();
            AddChild(panel);
            viewer = new GraphViewer();
            viewer.BindToPanel(panel);
        }

        /// <summary>
        /// Force select a node on the graph by <paramref name="uid"/>.
        /// </summary>
        public void SelectNode(string uid) {
            Node node = viewer.Graph.FindNode(uid);
            if (node == null) {
                return;
            }

            node.Attr.LineWidth = 2;
            Dispatcher.BeginInvoke(() => { // Call after the graph was redrawn
                OnLeftClick?.Invoke(node);
            });
        }

        /// <summary>
        /// Context menu options pass the selected node in their <paramref name="sender"/> parameter. Use this function to get the actually
        /// selected node, not the one that was last left-clicked. For edges, the source node will be selected (new nodes are intuitively
        /// then can be added after that to be between the edge's endpoints).
        /// </summary>
        public StyledNode GetSelectedNode(object sender) {
            if (sender is StyledNode hoverNode) { // Context menu, node = parallel
                return hoverNode;
            } else if (sender is Edge edge) { // Context menu, edge = inline
                return (StyledNode)viewer.Graph.FindNode(edge.Source);
            } else { // Window
                return SelectedNode;
            }
        }

        /// <summary>
        /// Hack to provide a Click event for MSAGL's WPF library.
        /// </summary>
        protected override void OnPreviewMouseUp(MouseButtonEventArgs e) {
            IViewerObject element = viewer.ObjectUnderMouseCursor;
            object param = null;
            if (element is IViewerNode vnode) {
                param = vnode.Node;
            } else if (element is IViewerEdge edge) {
                param = edge.Edge;
            }

            if (e.ChangedButton == MouseButton.Left) {
                StyledNode node = SelectedNode;
                if (node != null) {
                    node.Attr.LineWidth = 1; // Nodes selected with SelectNode are not actually selected, just were widened
                }
                Dispatcher.BeginInvoke(() => { // Call after the graph has handled it
                    OnLeftClick?.Invoke(param);
                });
            } else {
                OnRightClick?.Invoke(param);
            }
        }

        /// <summary>
        /// Hack to disable drag and drop as positions can't be preserved between graph updates.
        /// </summary>
        protected override void OnPreviewMouseMove(MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                e.Handled = true;
            }
        }
    }
}
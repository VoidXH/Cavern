using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using Cavern.QuickEQ.Equalization;

namespace Cavern.WPF.Utils {
    /// <summary>
    /// A band existing in an <see cref="Equalizer"/>. The base class is a band that was not yet added to any <see cref="Equalizer"/>.
    /// </summary>
    public class BandProxy {
        /// <summary>
        /// Position of the band in Hz.
        /// </summary>
        public virtual double Frequency { get; set; }

        /// <summary>
        /// Gain at <see cref="Frequency"/> in decibels.
        /// </summary>
        public virtual double Gain { get; set; }

        /// <summary>
        /// Get the <see cref="Equalizer"/>-compatible representation.
        /// </summary>
        public Band ToBand() => new Band(Frequency, Gain);
    }

    /// <summary>
    /// Edits a single band from an <see cref="Equalizer"/>.
    /// </summary>
    /// <param name="source">The <see cref="Equalizer"/> instance to modify</param>
    /// <param name="index">Index of the band when the linking was performed</param>
    /// <param name="reload">Called when a modification was performed - all <see cref="LinkedBandProxy"/>s shall be recreated then,
    /// because the order of the bands could have changed</param>
    public class LinkedBandProxy(Equalizer source, int index, Action reload) : BandProxy {
        /// <inheritdoc/>
        public override double Frequency {
            get => source.Bands[index].Frequency;
            set {
                Band oldBand = source.Bands[index];
                source.RemoveBand(oldBand);
                source.AddBand(new Band(value, oldBand.Gain));
                reload();
            }
        }

        /// <inheritdoc/>
        public override double Gain {
            get => source.Bands[index].Gain;
            set {
                Band oldBand = source.Bands[index];
                source.RemoveBand(oldBand);
                source.AddBand(new Band(oldBand.Frequency, value));
                reload();
            }
        }
    }

    /// <summary>
    /// Allows a <see cref="DataGrid"/> to edit an <see cref="Equalizer"/> instance.
    /// </summary>
    public class EqualizerToDataGrid : ObservableCollection<BandProxy>, IDisposable {
        /// <summary>
        /// The <see cref="DataGrid"/> displaying the <see cref="Band"/>s of the <see cref="source"/>.
        /// </summary>
        readonly DataGrid dataGrid;

        /// <summary>
        /// The <see cref="Equalizer"/> under editing.
        /// </summary>
        readonly Equalizer source;

        /// <summary>
        /// Called when an update was performed, UI can be updated with this event on changes.
        /// </summary>
        public event Action Updated;

        /// <summary>
        /// Allows a <see cref="DataGrid"/> to edit an <see cref="Equalizer"/> instance.
        /// </summary>
        public EqualizerToDataGrid(DataGrid dataGrid, Equalizer source) {
            this.source = source;
            dataGrid.RowEditEnding += EditEnding;
            dataGrid.KeyDown += KeyDown;
            Reload();
        }

        /// <inheritdoc/>
        public void Dispose() {
            GC.SuppressFinalize(this);
            dataGrid.RowEditEnding -= EditEnding;
        }

        /// <summary>
        /// Called when the user has added a new <see cref="Band"/> to the <see cref="Equalizer"/>. Because at this point,
        /// the <see cref="BandProxy"/> doesn't have its values set, a commit has to be forced to do it.
        /// </summary>
        void EditEnding(object sender, DataGridRowEditEndingEventArgs e) {
            if (e.EditAction != DataGridEditAction.Commit) {
                return;
            }
            DataGrid dataGrid = (DataGrid)sender;
            dataGrid.Dispatcher.BeginInvoke(() => {
                if (dataGrid.CommitEdit(DataGridEditingUnit.Row, true)) {
                    Reload();
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// Handle when the user wants to delete the selected band by pressing Backspace or Delete.
        /// </summary>
        void KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Back || e.Key == Key.Delete) {
                DataGrid dataGrid = (DataGrid)sender;
                if (dataGrid.SelectedItem is BandProxy proxy) {
                    source.RemoveBand(proxy.ToBand());
                }
                Reload();
            }
        }

        /// <summary>
        /// Update the layout when new bands were added or the order of the bands might have changed.
        /// </summary>
        void Reload() {
            for (int i = 0, c = Items.Count; i < c; i++) {
                if (Items[i] is not LinkedBandProxy) {
                    source.AddBand(Items[i].ToBand());
                }
            }
            Clear();
            IReadOnlyList<Band> bands = source.Bands;
            for (int i = 0, c = bands.Count; i < c; i++) {
                Add(new LinkedBandProxy(source, i, Reload));
            }
            Updated?.Invoke();
        }
    }
}
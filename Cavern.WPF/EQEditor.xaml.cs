using System.Windows;
using System.Windows.Controls;

using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Graphing.Overlays;
using Cavern.WPF.BaseClasses;
using Cavern.WPF.Utils;

namespace Cavern.WPF {
    /// <summary>
    /// Visual and table-based <see cref="Equalizer"/> editor.
    /// </summary>
    public partial class EQEditor : OkCancelDialog {
        /// <summary>
        /// The EQ curve under modification.
        /// </summary>
        readonly Equalizer equalizer;

        /// <summary>
        /// The initial EQ curve in case the user cancels the operation.
        /// </summary>
        readonly Equalizer original;

        /// <summary>
        /// Source of language strings.
        /// </summary>
        readonly ResourceDictionary language = Consts.Language.GetEQEditorStrings();

        /// <summary>
        /// Visual and table-based <see cref="Equalizer"/> editor.
        /// </summary>
        public EQEditor(Equalizer equalizer) {
            Resources.MergedDictionaries.Add(language);
            Resources.MergedDictionaries.Add(Consts.Language.GetCommonStrings());

            InitializeComponent();
            this.equalizer = equalizer;
            original = (Equalizer)equalizer.Clone();
            image.Overlay = new LogScaleGrid(2, 1, 0xFFAAAAAA, 10);
            image.AddCurve(equalizer, 0x0000FF);

            EqualizerToDataGrid bandHandler = new(bands, equalizer);
            bands.ItemsSource = bandHandler;
            bandHandler.Updated += OnUpdate;
        }

        /// <inheritdoc/>
        protected override void Cancel(object _, RoutedEventArgs e) {
            equalizer.ClearBands();
            IReadOnlyList<Band> bands = original.Bands;
            for (int i = 0, c = bands.Count; i < c; i++) {
                equalizer.AddBand(bands[i]);
            }
            base.Cancel(_, e);
        }

        /// <summary>
        /// Apply localization to the headers of property editor columns.
        /// </summary>
        void OnPropertyColumnsGenerating(object _, DataGridAutoGeneratingColumnEventArgs e) {
            switch (e.PropertyName) {
                case nameof(BandProxy.Frequency):
                    e.Column.Header = (string)language["TFreq"];
                    break;
                case nameof(BandProxy.Gain):
                    e.Column.Header = (string)language["TGain"];
                    break;
            }
        }

        /// <summary>
        /// Redraw the <see cref="image"/> when the <see cref="equalizer"/> <see cref="curve"/> was updated.
        /// </summary>
        void OnUpdate() => image.Invalidate();
    }
}
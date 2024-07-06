using System.Windows;
using System.Windows.Controls;

using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Graphing.Overlays;
using Cavern.QuickEQ.Graphing;
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
        /// Displays the spectrum on the UI.
        /// </summary>
        GraphRenderer renderer;

        /// <summary>
        /// The graphic representation of the <see cref="equalizer"/>.
        /// </summary>
        RenderedCurve curve;

        /// <summary>
        /// Visual and table-based <see cref="Equalizer"/> editor.
        /// </summary>
        public EQEditor(Equalizer equalizer) {
            Resources.MergedDictionaries.Add(language);
            Resources.MergedDictionaries.Add(Consts.Language.GetCommonStrings());
            InitializeComponent();
            this.equalizer = equalizer;
            original = (Equalizer)equalizer.Clone();
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
        /// Recreate the <see cref="renderer"/> in the current resolution.
        /// </summary>
        void OnResize(object _, SizeChangedEventArgs e) {
            if (curve != null) {
                curve = null;
            }
            renderer = new((int)(grid.ColumnDefinitions[0].ActualWidth + .5), (int)(grid.RowDefinitions[0].ActualHeight + .5)) {
                DynamicRange = 50,
                Peak = 25,
                Overlay = new LogScaleGrid(2, 1, 0xFF777777, 10)
            };
            OnUpdate();
        }

        /// <summary>
        /// Redraw the <see cref="image"/> when the <see cref="equalizer"/> <see cref="curve"/> was updated.
        /// </summary>
        void OnUpdate() {
            if (curve == null) {
                curve = renderer.AddCurve(equalizer, 0x0000FF);
            } else {
                curve.Update(true);
            }
            image.Source = renderer.Pixels.ToBitmap(renderer.Width, renderer.Height).ToImageSource();
        }
    }
}
using Microsoft.Win32;
using System.Windows;

using Cavern.Filters.Interfaces;
using Cavern.Format;
using Cavern.QuickEQ;
using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Graphing.Overlays;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;
using Cavern.WPF.BaseClasses;

namespace Cavern.WPF {
    /// <summary>
    /// Displays properties of a convolution's impulse response and allows loading a new set of samples.
    /// </summary>
    public partial class ConvolutionEditor : OkCancelDialog, IConvolution {
        /// <summary>
        /// Sample rate of the <see cref="impulse"/>.
        /// </summary>
        public int SampleRate {
            get => sampleRate;
            set => throw new InvalidOperationException();
        }
        int sampleRate;

        /// <summary>
        /// Last displayed/loaded impulse response of a convolution filter.
        /// </summary>
        public float[] Impulse {
            get => impulse;
            set {
                impulse = value;
                Reset();
            }
        }
        float[] impulse;

        /// <inheritdoc/>
        public int Delay { get; set; }

        /// <summary>
        /// The initial value of <see cref="impulse"/> as received in the constructor. When the editing is cancelled,
        /// or no new convolution samples are loaded, <see cref="Impulse"/> will return its original reference.
        /// </summary>
        readonly float[] originalImpulse;

        /// <summary>
        /// Source of language strings.
        /// </summary>
        readonly ResourceDictionary language = Consts.Language.GetConvolutionEditorStrings();

        /// <summary>
        /// Source of common language strings.
        /// </summary>
        readonly ResourceDictionary common = Consts.Language.GetCommonStrings();

        /// <summary>
        /// Displays properties of a convolution's impulse response and allows loading a new set of samples.
        /// </summary>
        public ConvolutionEditor(float[] impulse, int sampleRate) {
            Resources.MergedDictionaries.Add(language);
            Resources.MergedDictionaries.Add(common);
            InitializeComponent();
            impulseDisplay.Overlay = new Grid(2, 1, 0xFFAAAAAA, 10, 10);
            fftDisplay.Overlay = new LogScaleGrid(2, 1, 0xFFAAAAAA, 10);
            this.sampleRate = sampleRate;
            Impulse = impulse;
            originalImpulse = impulse;
        }

        /// <inheritdoc/>
        protected override void Cancel(object _, RoutedEventArgs e) {
            impulse = originalImpulse;
            base.Cancel(_, e);
        }

        /// <summary>
        /// Overwrite the convolution from an impulse response from a file, only allowing the system sample rate.
        /// </summary>
        void LoadFromFile(object _, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog() {
                Filter = string.Format((string)common["ImFmt"], AudioReader.filter)
            };
            if (dialog.ShowDialog().Value) {
                AudioReader file = AudioReader.Open(dialog.FileName);
                file.ReadHeader();
                if (file.ChannelCount != 1) {
                    Consts.Language.Error((string)language["EMono"]);
                } else {
                    sampleRate = file.SampleRate;
                    Impulse = file.Read();
                }
            }
        }

        /// <summary>
        /// Reanalyze the <see cref="impulse"/> and redraw the layout.
        /// </summary>
        void Reset() {
            impulseDisplay.Clear();
            fftDisplay.Clear();
            if (impulse == null) {
                polarity.Text = string.Empty;
                phaseDisplay.Text = string.Empty;
                delay.Text = string.Empty;
                return;
            }

            int maxFreq = Math.Min(sampleRate >> 1, 20000);
            impulseDisplay.AddCurve(EQGenerator.FromGraph(impulse, 20, maxFreq), 0xFF0000FF);
            Complex[] fft = impulse.FFT();
            fftDisplay.AddCurve(EQGenerator.FromTransferFunction(fft, sampleRate), 0xFF00FF00);
            float[] phase = Measurements.GetPhase(fft);
            float[] phaseGraph = GraphUtils.ConvertToGraph(phase, 20, maxFreq, sampleRate, 1024);
            WaveformUtils.Gain(phaseGraph, 25 / MathF.PI);
            fftDisplay.AddCurve(EQGenerator.FromGraph(phaseGraph, 20, maxFreq), 0xFFFF0000);

            VerboseImpulseResponse verbose = new(impulse);
            polarity.Text = string.Format((string)language["VPola"], verbose.Polarity ? '+' : '-');
            phaseDisplay.Text = string.Format((string)language["VPhas"], (180 / Math.PI * verbose.Phase).ToString("0.00"));
            delay.Text = string.Format((string)language["VDela"], verbose.Delay);
        }
    }
}
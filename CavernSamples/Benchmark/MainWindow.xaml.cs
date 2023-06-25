using System;
using System.Windows;

using Cavern.Utilities;

using Benchmark.Benchmarks;
using VoidX.WPF;

namespace Benchmark {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        readonly TaskEngine runner;

        /// <summary>
        /// Construct the window and create the task engine.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            cavernAmp.IsEnabled = CavernAmp.Available;
            runner = new TaskEngine(progress, null, status);
        }

        /// <summary>
        /// Free up resources when the window is closed.
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            runner?.Dispose();
            base.OnClosed(e);
        }

        /// <summary>
        /// Perform a benchmark and display performance metrics.
        /// </summary>
        /// <param name="procedureSetup">The generator of the procedure performing instance.</param>
        void Benchmark(Func<Benchmarks.Benchmark> procedureSetup) {
            CavernAmp.Bypass = !cavernAmp.IsChecked.Value;
            Benchmarks.Benchmark procedure = procedureSetup();
            int secs = seconds.Value;
            DateTime endTime = DateTime.Now + TimeSpan.FromSeconds(secs);
            int steps = 0;
            runner.Run(() => {
                DateTime now = DateTime.Now;
                while (now < endTime) {
                    ++steps;
                    procedure.Step();
                    now = DateTime.Now;
                    runner.UpdateProgressBar(1 - (endTime - now).TotalSeconds / secs);
                }
                MessageBox.Show($"Performed {steps} steps in {secs} seconds. " +
                    $"Performance: {procedure.ToString(steps, secs)}.", "Benchmark finished");
            });
        }

        /// <summary>
        /// Perform a convolution benchmark.
        /// </summary>
        void StartConvolution(object sender, RoutedEventArgs e) =>
            Benchmark(() => new Convolution(filterSize.Value, blockSize.Value, channels.Value));
    }
}
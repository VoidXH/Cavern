using Benchmark.Benchmarks;
using System;
using System.Windows;
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
            runner = new TaskEngine(progress, status);
        }

        /// <summary>
        /// Perform a benchmark and display performance metrics.
        /// </summary>
        void Benchmark(Benchmarks.Benchmark benchmark) {
            int secs = seconds.Value;
            DateTime endTime = DateTime.Now + TimeSpan.FromSeconds(secs);
            int steps = 0;
            runner.Run(() => {
                DateTime now = DateTime.Now;
                while (now < endTime) {
                    ++steps;
                    benchmark.Step();
                    now = DateTime.Now;
                    runner.UpdateProgressBar(1 - (endTime - now).TotalSeconds / secs);
                }
                MessageBox.Show($"Performed {steps} steps in {secs} seconds. " +
                    $"Performance: {benchmark.ToString(steps, secs)}.", "Benchmark finished");
            });
        }

        /// <summary>
        /// Perform a convolution benchmark.
        /// </summary>
        void StartConvolution(object sender, RoutedEventArgs e) => Benchmark(new Convolution(filterSize.Value));
    }
}
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;

using Cavern;
using Cavern.Channels;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Utilities;
using VoidX.WPF;
using VoidX.WPF.FFmpeg;

using Cavernize.Logic.CavernSettings;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;
using CavernizeGUI.CavernSettings;
using CavernizeGUI.Elements;
using CavernizeGUI.Resources;
using CavernizeGUI.Windows;

using Path = System.IO.Path;

namespace CavernizeGUI {
    public partial class MainWindow : Window, ICavernizeApp {
        /// <summary>
        /// Source of language strings.
        /// </summary>
        public static readonly ResourceDictionary language = Consts.Language.GetMainWindowStrings();

        /// <inheritdoc/>
        public bool Rendering => taskEngine.IsOperationRunning;

        /// <inheritdoc/>
        public string FilePath => file?.Path;

        /// <inheritdoc/>
        public ExportFormat ExportFormat {
            get => (ExportFormat)audio.SelectedItem;
            set => audio.SelectedItem = value;
        }

        /// <inheritdoc/>
        public RenderTarget RenderTarget {
            get => (RenderTarget)renderTarget.SelectedItem;
            set => renderTarget.SelectedItem = value;
        }

        /// <inheritdoc/>
        public Cavern.CavernSettings.UpmixingSettings UpmixingSettings { get; } = new DynamicUpmixingSettings();

        /// <inheritdoc/>
        public SpecialRenderModeSettings SpecialRenderModeSettings { get; }

        /// <summary>
        /// The fields that show property-value pairs in a table.
        /// </summary>
        readonly (TextBlock property, TextBlock value)[] trackInfo;

        /// <summary>
        /// Render process handler.
        /// </summary>
        readonly ConversionEnvironment environment;

        /// <summary>
        /// FFmpeg runner and locator.
        /// </summary>
        readonly FFmpegGUI ffmpeg;

        /// <summary>
        /// Queued conversions.
        /// </summary>
        readonly ObservableCollection<QueuedJob> jobs = [];

        /// <summary>
        /// Runs the process in the background.
        /// </summary>
        readonly TaskEngine taskEngine;

        /// <summary>
        /// Currently loaded audio file.
        /// </summary>
        AudioFile file;

        /// <summary>
        /// Any setting has changed in the application and it should be saved.
        /// </summary>
        bool settingChanged;

        /// <summary>
        /// One-time UI transformations were applied.
        /// </summary>
        bool uiInitialized;

        /// <summary>
        /// Minimum window width that displays the queue. The window is resized to this width when a queue item is added.
        /// </summary>
        double minWidth;

        /// <summary>
        /// Initialize the window and load last settings.
        /// </summary>
        public MainWindow() {
            TaskbarItemInfo = new TaskbarItemInfo {
                ProgressState = TaskbarItemProgressState.Normal
            };

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", Path.Combine(appData, "Temp", "CavernizeWebCache"));
            InitializeComponent();
            SpecialRenderModeSettings = new DynamicSpecialRenderModeSettings();
            trackInfo = [
                (trackTable1Title, trackTable1Value),
                (trackTable2Title, trackTable2Value),
                (trackTable3Title, trackTable3Value)
            ];
#if DEBUG
            MenuItem testLanguage = new MenuItem {
                Header = "Test",
            };
            testLanguage.Click += LanguageTest;
            languages.Items.Add(testLanguage);
#endif

            ExportFormat[] formats = ExportFormat.GetFormats(Consts.Language.GetTrackStrings());
            audio.ItemsSource = formats;
            audio.SelectedIndex = Math.Clamp(Settings.Default.outputCodec + 2, 0, formats.Length);

            FFmpeg.ReadyText = (string)language["FFRea"];
            FFmpeg.NotReadyText = (string)language["FFNRe"];
            ffmpeg = new FFmpegGUI(status, Settings.Default.ffmpegLocation);
            if (ffmpeg.Found) {
                locateFFmpeg.Visibility = Visibility.Hidden;
            }

            environment = new(this);
            renderTarget.ItemsSource = RenderTarget.Targets;
            renderTarget.SelectedIndex = Math.Clamp(Settings.Default.renderTarget + 4, 0, RenderTarget.Targets.Length - 1);
            renderSettings.IsEnabled = true; // Don't grey out initially
            queuedJobs.ItemsSource = jobs;
            taskEngine = new(progress, TaskbarItemInfo, status);
            Reset();

            if (File.Exists(Settings.Default.hrirPath)) {
                hrir.IsChecked = TryLoadHRIR(false);
            }

            speakerVirtualizer.IsChecked = Settings.Default.speakerVirtualizer;
            force24Bit.IsChecked = Settings.Default.force24Bit;
            surroundSwap.IsChecked = Settings.Default.surroundSwap;
            wavChannelSkip.IsChecked = Settings.Default.wavChannelSkip;
            checkUpdates.IsChecked = Settings.Default.checkUpdates;
            if (Settings.Default.checkUpdates && !Program.ConsoleMode) {
                UpdateCheck.Perform(Settings.Default.lastUpdate, () => Settings.Default.lastUpdate = DateTime.Now);
            }
            Settings.Default.SettingChanging += (_, e) => settingChanged |= !Settings.Default[e.SettingName].Equals(e.NewValue);
        }

        /// <summary>
        /// Displays an error message.
        /// </summary>
        static void Error(string message) {
            if (Program.ConsoleMode) {
                Console.Error.WriteLine(message);
            } else {
                MessageBox.Show(message, (string)language["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Perform one-time UI updates after the window is initialized and displayed.
        /// </summary>
        protected override void OnActivated(EventArgs e) {
            if (!uiInitialized) {
                minWidth = Width;
                Width = queuedJobs.TransformToAncestor(this).Transform(new Point()).X;
                uiInitialized = true;
            }
        }

        /// <inheritdoc/>
        public void OpenContent(string path) {
            Reset();
            ffmpeg.CheckFFmpeg();
            taskEngine.Progress = 0;
            OnOutputSelected(null, null);

#if RELEASE
            try {
#endif
            OpenContent(new AudioFile(path, Consts.Language.GetTrackStrings()));
#if RELEASE
            } catch (IOException e) {
                Reset();
                throw new Exception(e.Message);
            } catch (Exception e) {
                Reset();
                throw new Exception($"{e.Message} {Consts.Language.GetTrackStrings().Later}");
            }
#endif
            Settings.Default.lastDirectory = Path.GetDirectoryName(path);
        }

        /// <inheritdoc/>
        public void OpenContent(AudioFile file) {
            fileName.Text = Path.GetFileName(file.Path);
            this.file = file;
            if (file.Tracks.Count != 0) {
                trackControls.Visibility = Visibility.Visible;
                tracks.ItemsSource = file.Tracks;
                tracks.SelectedIndex = 0;
                // Prioritize spatial codecs
                for (int i = 0, c = file.Tracks.Count; i < c; i++) {
                    if (file.Tracks[i].Codec == Codec.EnhancedAC3) {
                        tracks.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void RenderContent(string path) {
            Action renderTask;
#if RELEASE
            try {
#endif
                PreRender();
                renderTask = Render(path);
#if RELEASE
            } catch (Exception e) {
                Error(e.Message);
                return;
            }
#endif

            if (renderTask != null) {
                taskEngine.Run(renderTask, Error);
            }
        }

        /// <summary>
        /// Save persistent settings and free resources on quit.
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            taskEngine?.Dispose();
            Settings.Default.ffmpegLocation = ffmpeg.Location;
            Settings.Default.renderTarget = renderTarget.SelectedIndex - 4;
            Settings.Default.outputCodec = audio.SelectedIndex - 2;
            Settings.Default.speakerVirtualizer = speakerVirtualizer.IsChecked;
            Settings.Default.force24Bit = force24Bit.IsChecked;
            Settings.Default.checkUpdates = checkUpdates.IsChecked;
            if (settingChanged) {
                Settings.Default.Save();
            }
            base.OnClosed(e);
        }

        /// <summary>
        /// Reset the listener and remove the objects of the last render.
        /// </summary>
        void Reset() {
            environment.Reset();
            if (file != null && jobs.FirstOrDefault(x => x.IsUsingFile(file)) == null) {
                file.Dispose();
                file = null;
            }
            fileName.Text = string.Empty;
            trackControls.Visibility = Visibility.Hidden;
            tracks.ItemsSource = null;
            trackCodec.Text = string.Empty;
            for (int i = 0; i < trackInfo.Length; i++) {
                trackInfo[i].property.Text = string.Empty;
                trackInfo[i].value.Text = string.Empty;
            }
            report = new(environment.Listener, Consts.Language.GetRenderReportStrings());
        }

        /// <summary>
        /// Open file button event; loads an audio file to <see cref="reader"/>.
        /// </summary>
        void OpenFile(object _, RoutedEventArgs e) {
            if (Rendering) {
                Error((string)language["OpRun"]);
                return;
            }

            OpenFileDialog dialog = new() {
                Filter = string.Format((string)language["ImFmt"], AudioReader.filter)
            };
            if (Directory.Exists(Settings.Default.lastDirectory)) {
                dialog.InitialDirectory = Settings.Default.lastDirectory;
            }
            if (dialog.ShowDialog().Value) {
#if RELEASE
                try {
#endif
                    OpenContent(dialog.FileName);
#if RELEASE
                } catch (Exception ex) {
                    Error(ex.Message);
                }
#endif
            }
        }

        /// <summary>
        /// When a file is dropped on the Content processing box, open it.
        /// </summary>
        void DropFile(object _, DragEventArgs e) {
            if (e.Data is DataObject obj && obj.ContainsFileDropList()) {
                StringCollection files = obj.GetFileDropList();
                if (files.Count == 1) {
                    OpenContent(files[0]);
                } else {
                    Error((string)language["DropF"]);
                }
            }
        }

        /// <summary>
        /// Display the selected render target's active channels.
        /// </summary>
        void OnRenderTargetSelected(object _, SelectionChangedEventArgs e) {
            RenderTarget selected = RenderTarget;
            if (selected is DriverRenderTarget || selected is VirtualizerRenderTarget) {
                wiring.IsEnabled = false;
                layoutDisplay.All = dynamicSpeaker;
                return;
            }

            wiring.IsEnabled = true;
            layoutDisplay.All = inactiveSpeaker;

            ReferenceChannel[] channels = selected.Channels;
            for (int ch = 0; ch < channels.Length; ch++) {
                if (selected.IsExported(ch) && channels[ch] != ReferenceChannel.ScreenLFE) {
                    layoutDisplay[channels[ch]] = activeSpeaker;
                }
            }
        }

        /// <summary>
        /// Open the render target selector at the render target's position when clicking the render target dropdown.
        /// </summary>
        void OnRenderTargetOpened(object sender, EventArgs e) {
            renderTarget.IsDropDownOpen = false;
            Point renderTargetPos = renderTarget.PointToScreen(new Point(0, 0));
            RenderTargetSelector selector = new(RenderTarget.Targets, RenderTarget) {
                Left = renderTargetPos.X,
                Top = renderTargetPos.Y + renderTarget.ActualHeight
            };
            selector.ShowDialog();
            renderTarget.SelectedItem = selector.Result;
        }

        /// <summary>
        /// Display track metadata on track selection.
        /// </summary>
        void OnTrackSelected(object _, SelectionChangedEventArgs e) {
            if (tracks.SelectedItem != null) {
                CavernizeTrack track = (CavernizeTrack)tracks.SelectedItem;
                trackCodec.Text = track.FormatHeader;
                (string property, string value)[] details = track.Details;
                int fill = Math.Min(trackInfo.Length, details.Length);
                for (int i = 0; i < fill; i++) {
                    trackInfo[i].property.Text = details[i].property;
                    trackInfo[i].value.Text = details[i].value;
                }
                for (int i = fill; i < trackInfo.Length; i++) {
                    trackInfo[i].property.Text = string.Empty;
                    trackInfo[i].value.Text = string.Empty;
                }
            }
        }

        /// <summary>
        /// Grey out renderer settings when it's not applicable.
        /// </summary>
        void OnOutputSelected(object _, SelectionChangedEventArgs e) =>
            renderSettings.IsEnabled = !ExportFormat.Codec.IsEnvironmental();

        /// <summary>
        /// Prompt the user to select FFmpeg's installation folder.
        /// </summary>
        void LocateFFmpeg(object _, RoutedEventArgs e) {
            if (taskEngine.IsOperationRunning) {
                Error((string)language["OpRun"]);
                return;
            }

            ffmpeg.Locate();
        }

        /// <summary>
        /// Start the rendering process.
        /// </summary>
        void Render(object _, RoutedEventArgs e) {
            Action renderTask = GetRenderTask();
            if (renderTask != null) {
                taskEngine.Run(renderTask, Error);
            }
        }

        /// <summary>
        /// Handle when a dragged file moves over a control that supports drop.
        /// </summary>
        void FileDragEnter(object _, DragEventArgs e) {
            var dropPossible = e.Data != null && ((DataObject)e.Data).ContainsFileDropList();
            if (dropPossible) {
                e.Effects = DragDropEffects.Copy;
            }
        }

        /// <summary>
        /// Handle when a dragged file leaves a control that supports drop.
        /// </summary>
        void FileDragOver(object _, DragEventArgs e) => e.Handled = true;

        /// <summary>
        /// Opens the software's documentation.
        /// </summary>
        void Guide(object _, RoutedEventArgs e) => Process.Start(new ProcessStartInfo {
            FileName = "https://cavern.sbence.hu/cavern/doc.php?p=Cavernize",
            UseShellExecute = true
        });

        /// <summary>
        /// Shows information about the used Cavern library and its version.
        /// </summary>
        void About(object _, RoutedEventArgs e) {
            StringBuilder result = new StringBuilder(Listener.Info);
            if (CavernAmp.Available) {
                result.Append('\n').Append((string)language["AbouA"]);
            }
            MessageBox.Show(result.ToString(), (string)language["AbouH"]);
        }

        /// <summary>
        /// Open Cavern's website.
        /// </summary>
        void Ad(object _, RoutedEventArgs e) => Process.Start(new ProcessStartInfo {
            FileName = "https://cavern.sbence.hu",
            UseShellExecute = true
        });

        /// <summary>
        /// Color used for active speaker display.
        /// </summary>
        static readonly SolidColorBrush activeSpeaker = new(Color.FromRgb(0x31, 0x86, 0xCE));

        /// <summary>
        /// Color used for speaker display when a dynamic render target is selected.
        /// </summary>
        static readonly SolidColorBrush dynamicSpeaker = new(Colors.Beige);

        /// <summary>
        /// Color used for inactive speaker display.
        /// </summary>
        static readonly SolidColorBrush inactiveSpeaker = new(Colors.Gray);
    }
}
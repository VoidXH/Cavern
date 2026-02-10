using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;

using Cavern;
using Cavern.Channels;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Utilities;
using VoidX.WPF;
using VoidX.WPF.FFmpeg;

using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;
using CavernizeGUI.CavernSettings;
using CavernizeGUI.Consts;
using CavernizeGUI.Resources;
using CavernizeGUI.Windows;

using Path = System.IO.Path;

namespace CavernizeGUI;

public partial class MainWindow : Window {
    /// <summary>
    /// Source of language strings.
    /// </summary>
    public static readonly ResourceDictionary language = Consts.Language.GetMainWindowStrings();

    /// <summary>
    /// Render process handler.
    /// </summary>
    readonly ConversionEnvironment environment;

    /// <summary>
    /// FFmpeg runner and locator.
    /// </summary>
    readonly FFmpegGUI ffmpeg;

    /// <summary>
    /// Queued jobs.
    /// </summary>
    readonly Queue queue;

    /// <summary>
    /// Runs the process in the background.
    /// </summary>
    readonly TaskEngine taskEngine;

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
        RenderingSettings = new DynamicSpecialRenderModeSettings();
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
        renderTarget.SelectedIndex = Math.Clamp(Settings.Default.renderTarget + 6, 0, RenderTarget.Targets.Length - 1);
        renderSettings.IsEnabled = true; // Don't grey out initially
        queue = new(this);
        queuedJobs.ItemsSource = queue.Jobs;
        taskEngine = new(progress, TaskbarItemInfo, status);
        Reset();

        if (File.Exists(Settings.Default.hrirPath)) {
            hrir.IsChecked = TryLoadHRIR(false);
        }

        speakerVirtualizer.IsChecked = Settings.Default.speakerVirtualizer;
        force24Bit.IsChecked = Settings.Default.force24Bit;
        surroundSwap.IsChecked = SurroundSwap;
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

    /// <summary>
    /// Save persistent settings and free resources on quit.
    /// </summary>
    protected override void OnClosed(EventArgs e) {
        LoadedFile?.Dispose();
        taskEngine?.Dispose();
        Settings.Default.ffmpegLocation = ffmpeg.Location;
        Settings.Default.renderTarget = renderTarget.SelectedIndex - 6;
        Settings.Default.outputCodec = audio.SelectedIndex - 2;
        Settings.Default.speakerVirtualizer = speakerVirtualizer.IsChecked;
        Settings.Default.force24Bit = force24Bit.IsChecked;
        Settings.Default.checkUpdates = checkUpdates.IsChecked;
        if (settingChanged) {
            Settings.Default.Save();
        }
        CheckBlocks();
        base.OnClosed(e);
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
            try {
                OpenContent(dialog.FileName);
            } catch (Exception ex) {
                Error(ex.Message);
            }
        }
    }

    /// <summary>
    /// When a file is dropped on the Content processing box, open it.
    /// </summary>
    void DropFile(object _, DragEventArgs e) {
        if (e.Data is DataObject obj && obj.ContainsFileDropList()) {
            StringCollection files = obj.GetFileDropList();
            if (files.Count == 1) {
                try {
                    OpenContent(files[0]);
                } catch (Exception ex) {
                    Error(ex.Message);
                }
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
            layoutDisplay.All = UI.dynamicSpeaker;
            return;
        }

        wiring.IsEnabled = true;
        layoutDisplay.All = UI.inactiveSpeaker;

        ReferenceChannel[] channels = selected.Channels;
        for (int ch = 0; ch < channels.Length; ch++) {
            if (selected.IsExported(ch) && channels[ch] != ReferenceChannel.ScreenLFE) {
                layoutDisplay[channels[ch]] = UI.activeSpeaker;
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
    void OnTrackSelected(object _, SelectionChangedEventArgs e) => SelectedTrack = (CavernizeTrack)tracks.SelectedItem;

    /// <summary>
    /// Grey out renderer settings when it's not applicable.
    /// </summary>
    void OnOutputSelected(object _, SelectionChangedEventArgs e) => renderSettings.IsEnabled = !ExportFormat.Codec.IsEnvironmental();

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
        Action renderTask = GetRenderTask(null);
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
}

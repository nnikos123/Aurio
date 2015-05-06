﻿using AudioAlign.Audio;
using AudioAlign.Audio.Matching;
using AudioAlign.Audio.Project;
using AudioAlign.Audio.TaskMonitor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AudioAlign.Test.FingerprintingBenchmark {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private ProgressMonitor progressMonitor;
        private ObservableCollection<BenchmarkEntry> benchmarkResults;

        public MainWindow() {
            InitializeComponent();
            progressMonitor = ProgressMonitor.GlobalInstance;
            benchmarkResults = new ObservableCollection<BenchmarkEntry>();
            dataGrid1.ItemsSource = benchmarkResults;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            progressBar1.IsEnabled = false;
            progressBar1Label.Content = "";
            progressMonitor.ProcessingStarted += ProgressMonitor_ProcessingStarted;
            progressMonitor.ProcessingProgressChanged += ProgressMonitor_ProcessingProgressChanged;
            progressMonitor.ProcessingFinished += ProgressMonitor_ProcessingFinished;
        }

        private void ProgressMonitor_ProcessingStarted(object sender, EventArgs e) {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate {
                progressBar1.IsEnabled = true;
            });
        }

        private void ProgressMonitor_ProcessingProgressChanged(object sender, Audio.ValueEventArgs<float> e) {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate {
                progressBar1.Value = e.Value;
                progressBar1Label.Content = progressMonitor.StatusMessage;
            });
        }

        private void ProgressMonitor_ProcessingFinished(object sender, EventArgs e) {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate {
                progressBar1.Value = 0;
                progressBar1.IsEnabled = false;
                progressBar1Label.Content = "";
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Filter = "Wave files|*.wav";

            if (dlg.ShowDialog() == true) {
                Benchmark(new AudioTrack(new System.IO.FileInfo(dlg.FileName)), warmupCheckbox.IsChecked == true);
            }
        }

        private void ReportBenchmarkResult(BenchmarkEntry e) {
            dataGrid1.Dispatcher.BeginInvoke((Action)delegate{
                benchmarkResults.Add(e);
            });
        }

        private void Benchmark(AudioTrack track, bool warmup) {
            Task.Factory.StartNew(() => {
                if (warmup) {
                    // "Warmup" reads the whole stream before starting the benchmark procedures
                    // to trigger the file caching in Windows, else the first fingerprinting run
                    // on a file is always slower than the following because the file is cached
                    // on successive runs.
                    var stream = track.CreateAudioStream();
                    var buffer = new byte[1024 * 1024];
                    int bytesRead = 0;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0) {
                        // nothing to do here
                    }
                }

                BenchmarkHaitsmaKalker(track);
                BenchmarkWang(track);
                BenchmarkEchoprint(track);
                BenchmarkChromaprint(track);
            });
        }

        private void BenchmarkHaitsmaKalker(AudioTrack track) {
            var profile = AudioAlign.Audio.Matching.HaitsmaKalker2002.FingerprintGenerator.GetProfiles()[0];
            var store = new AudioAlign.Audio.Matching.HaitsmaKalker2002.FingerprintStore(profile);
            var gen = new AudioAlign.Audio.Matching.HaitsmaKalker2002.FingerprintGenerator(profile, track, 3);

            var reporter = ProgressMonitor.GlobalInstance.BeginTask("HaitsmaKalker2002", true);
            int hashCount = 0;

            gen.SubFingerprintCalculated += delegate(object sender, SubFingerprintEventArgs e) {
                foreach (var subFingerprint in e.SubFingerprints) {
                    store.Add(e.AudioTrack, subFingerprint.SubFingerprint, subFingerprint.Index, subFingerprint.IsVariation);
                }
                hashCount++;
                reporter.ReportProgress((double)e.Index / e.Indices * 100);
            };

            Stopwatch sw = new Stopwatch();
            sw.Start();

            gen.Generate();

            sw.Stop();
            reporter.Finish();
            ReportBenchmarkResult(new BenchmarkEntry { Track = track, Type = "HaitsmaKalker2002", HashCount = hashCount, Duration = sw.Elapsed });
        }

        private void BenchmarkWang(AudioTrack track) {
            var profile = new AudioAlign.Audio.Matching.Wang2003.Profile();
            var store = new AudioAlign.Audio.Matching.Wang2003.FingerprintStore(profile);
            var gen = new AudioAlign.Audio.Matching.Wang2003.FingerprintGenerator(profile);

            var reporter = ProgressMonitor.GlobalInstance.BeginTask("Wang2003", true);
            int hashCount = 0;

            gen.FingerprintHashesGenerated += delegate(object sender, SubFingerprintEventArgs e) {
                store.Add(e);
                hashCount += e.SubFingerprints.Count;
                reporter.ReportProgress((double)e.Index / e.Indices * 100);
            };

            Stopwatch sw = new Stopwatch();
            sw.Start();

            gen.Generate(track);

            sw.Stop();
            reporter.Finish();
            ReportBenchmarkResult(new BenchmarkEntry { Track = track, Type = "Wang2003", HashCount = hashCount, Duration = sw.Elapsed });
        }

        private void BenchmarkEchoprint(AudioTrack track) {
            var profile = new AudioAlign.Audio.Matching.Echoprint.Profile();
            var store = new AudioAlign.Audio.Matching.Echoprint.FingerprintStore(profile);
            var gen = new AudioAlign.Audio.Matching.Echoprint.FingerprintGenerator(profile);

            var reporter = ProgressMonitor.GlobalInstance.BeginTask("Echoprint", true);
            int hashCount = 0;

            gen.FingerprintHashesGenerated += delegate(object sender, SubFingerprintEventArgs e) {
                store.Add(e);
                hashCount += e.SubFingerprints.Count;
                reporter.ReportProgress((double)e.Index / e.Indices * 100);
            };

            Stopwatch sw = new Stopwatch();
            sw.Start();

            gen.Generate(track);

            sw.Stop();
            reporter.Finish();
            ReportBenchmarkResult(new BenchmarkEntry { Track = track, Type = "Echoprint", HashCount = hashCount, Duration = sw.Elapsed });
        }

        private void BenchmarkChromaprint(AudioTrack track) {
            var profile = AudioAlign.Audio.Matching.Chromaprint.FingerprintGenerator.GetProfiles()[0];
            var store = new AudioAlign.Audio.Matching.Chromaprint.FingerprintStore(profile);
            var gen = new AudioAlign.Audio.Matching.Chromaprint.FingerprintGenerator(profile);

            var reporter = ProgressMonitor.GlobalInstance.BeginTask("Chromaprint", true);
            int hashCount = 0;

            gen.SubFingerprintCalculated += delegate(object sender, SubFingerprintEventArgs e) {
                foreach (var subFingerprint in e.SubFingerprints) {
                    store.Add(e.AudioTrack, subFingerprint.SubFingerprint, subFingerprint.Index, subFingerprint.IsVariation);
                }
                hashCount++;
                reporter.ReportProgress((double)e.Index / e.Indices * 100);
            };

            Stopwatch sw = new Stopwatch();
            sw.Start();

            gen.Generate(track);

            sw.Stop();
            reporter.Finish();
            ReportBenchmarkResult(new BenchmarkEntry { Track = track, Type = "Chromaprint", HashCount = hashCount, Duration = sw.Elapsed });
        }
    }
}

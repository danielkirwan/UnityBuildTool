using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Forms = System.Windows.Forms; 
using MessageBox = System.Windows.MessageBox; 

namespace UnityBuild
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UnityVersionCombo.ItemsSource = GetUnityVersions();
        }

        private ObservableCollection<string> GetUnityVersions()
        {
            var unityHubPath = @"C:\Program Files\Unity\Hub\Editor";
            if (!Directory.Exists(unityHubPath))
                return new ObservableCollection<string>();

            return new ObservableCollection<string>(
                Directory.GetDirectories(unityHubPath)
                         .Select(Path.GetFileName)
                         .OrderByDescending(v => v)
            );
        }

        private void BrowseProject_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == Forms.DialogResult.OK)
                ProjectPathBox.Text = dlg.SelectedPath;
        }

        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == Forms.DialogResult.OK)
                BuildOutputBox.Text = dlg.SelectedPath;
        }

        private async void Build_Click(object sender, RoutedEventArgs e)
        {
            string unityVersion = UnityVersionCombo.SelectedItem as string;
            string projectPath = ProjectPathBox.Text;
            string outputPath = BuildOutputBox.Text;

            if (string.IsNullOrWhiteSpace(unityVersion) || string.IsNullOrWhiteSpace(projectPath))
            {
                MessageBox.Show("Please select a Unity version and project path.");
                return;
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.Combine(projectPath, "Builds");
                Directory.CreateDirectory(outputPath);
                BuildOutputBox.Text = outputPath;
            }

            string unityExe = $@"C:\Program Files\Unity\Hub\Editor\{unityVersion}\Editor\Unity.exe";

            if (!File.Exists(unityExe))
            {
                MessageBox.Show($"Unity executable not found:\n{unityExe}");
                return;
            }

            string arguments = $"-batchmode -quit -projectPath \"{projectPath}\" -executeMethod BuildAutomation.BuildPC -buildPath \"{outputPath}\"";

            BuildButton.IsEnabled = false;
            BuildProgressBar.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = unityExe,
                            Arguments = arguments,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.OutputDataReceived += (s, ev) =>
                    {
                        if (!string.IsNullOrEmpty(ev.Data))
                            Console.WriteLine(ev.Data);
                    };
                    process.ErrorDataReceived += (s, ev) =>
                    {
                        if (!string.IsNullOrEmpty(ev.Data))
                            Console.WriteLine(ev.Data);
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Build failed:\n{ex.Message}"));
                }
            });

            BuildButton.IsEnabled = true;
            BuildProgressBar.Visibility = Visibility.Collapsed;

            MessageBox.Show($"✅ Build completed!\nOutput folder:\n{outputPath}");
        }
    }
}

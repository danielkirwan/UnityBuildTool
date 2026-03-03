using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

            // Optional: auto-select newest version
            if (UnityVersionCombo.Items.Count > 0)
                UnityVersionCombo.SelectedIndex = 0;

            // Optional defaults
            if (string.IsNullOrWhiteSpace(BuildNameBox.Text))
                BuildNameBox.Text = "MyGame";

            if (string.IsNullOrWhiteSpace(VersionBox.Text))
                VersionBox.Text = "1.0.0";
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
            string projectPath = ProjectPathBox.Text?.Trim();
            string outputPath = BuildOutputBox.Text?.Trim();
            string buildName = BuildNameBox.Text?.Trim();
            string version = VersionBox.Text?.Trim();
            string buildType = (BuildTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "release";

            bool doDeleteSaves = DeleteSavesCheck.IsChecked == true;
            bool doResetScriptables = ResetScriptablesCheck.IsChecked == true;

            if (string.IsNullOrWhiteSpace(unityVersion) || string.IsNullOrWhiteSpace(projectPath))
            {
                MessageBox.Show("Please select a Unity version and project path.");
                return;
            }

            if (!Directory.Exists(projectPath))
            {
                MessageBox.Show("Project path does not exist.");
                return;
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.Combine(projectPath, "Builds");
                Directory.CreateDirectory(outputPath);
                BuildOutputBox.Text = outputPath;
            }

            if (string.IsNullOrWhiteSpace(buildName))
                buildName = "MyGame";

            if (string.IsNullOrWhiteSpace(version))
                version = "1.0.0";

            // Keep build names filesystem-safe
            buildName = string.Concat(buildName.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
            buildName = buildName.Replace(' ', '_');

            string unityExe = $@"C:\Program Files\Unity\Hub\Editor\{unityVersion}\Editor\Unity.exe";
            if (!File.Exists(unityExe))
            {
                MessageBox.Show($"Unity executable not found:\n{unityExe}");
                return;
            }

            // IMPORTANT:
            // - projectPath stays quoted
            // - use -key=value with NO spaces around '='
            // - quote values that may contain spaces (like outputPath)
            string arguments =
                $"-batchmode -quit " +
                $"-projectPath \"{projectPath}\" " +
                $"-executeMethod BuildAutomation.RunFromCli " +
                $"-buildPath=\"{outputPath}\" " +
                $"-buildName={buildName} " +
                $"-version={version} " +
                $"-buildType={buildType} " +
                $"-doDeleteSaves={(doDeleteSaves ? "true" : "false")} " +
                $"-doResetScriptables={(doResetScriptables ? "true" : "false")}";

            BuildButton.IsEnabled = false;
            BuildProgressBar.Visibility = Visibility.Visible;

            int exitCode = -1;

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
                            Console.WriteLine("UNITY: " + ev.Data);
                    };

                    process.ErrorDataReceived += (s, ev) =>
                    {
                        if (!string.IsNullOrEmpty(ev.Data))
                            Console.WriteLine("UNITY ERROR: " + ev.Data);
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Build failed:\n{ex.Message}"));
                }
            });

            BuildButton.IsEnabled = true;
            BuildProgressBar.Visibility = Visibility.Collapsed;

            if (exitCode == 0)
                MessageBox.Show($"✅ Build completed!\nOutput folder:\n{outputPath}");
            else
                MessageBox.Show($"⚠ Build finished with exit code {exitCode}.\nCheck Unity Editor.log for details.");
        }

        private void BrowseIconSource_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Image Files|*.png;*.jpg;*.jpeg";
            if (dlg.ShowDialog() == true)
                IconSourcePath.Text = dlg.FileName;
        }

        private void BrowseIconOutput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == Forms.DialogResult.OK)
                IconOutputPath.Text = dlg.SelectedPath;
        }

        private void GenerateIcons_Click(object sender, RoutedEventArgs e)
        {
            // (unchanged from your existing code)
            // ... keep your implementation here ...
        }

        // (unchanged helper methods below)
        // GenerateUnityIconSet, SaveResizedImage, GenerateWindowsICO, GenerateMacICNS, GenerateAndroidMipmaps...
        // Keep your existing implementations exactly as you have them.

        private async void ApplyIconsToUnity_Click(object sender, RoutedEventArgs e)
        {
            string unityVersion = UnityVersionCombo.SelectedItem as string;
            string projectPath = ProjectPathBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(unityVersion))
            {
                MessageBox.Show("Please select a Unity version.");
                return;
            }

            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            {
                MessageBox.Show("Please select a valid Unity project path.");
                return;
            }

            string iconsRoot = Path.Combine(projectPath, "Assets/Editor/GeneratedIcons");
            if (!Directory.Exists(iconsRoot))
            {
                MessageBox.Show("No generated icons found.\nPlease click 'Generate Icons' first.");
                return;
            }

            string unityExe = $@"C:\Program Files\Unity\Hub\Editor\{unityVersion}\Editor\Unity.exe";
            if (!File.Exists(unityExe))
            {
                MessageBox.Show($"Unity executable not found:\n{unityExe}");
                return;
            }

            // Use the same stable -key=value pattern
            string arguments =
                $"-batchmode -quit " +
                $"-projectPath \"{projectPath}\" " +
                $"-executeMethod IconAutomation.ApplyIcons " +
                $"-iconsRootPath=\"{iconsRoot}\"";

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
                    Console.WriteLine("UNITY: " + ev.Data);
            };

            process.ErrorDataReceived += (s, ev) =>
            {
                if (!string.IsNullOrEmpty(ev.Data))
                    Console.WriteLine("UNITY ERROR: " + ev.Data);
            };

            await Task.Run(() =>
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            });

            MessageBox.Show("Finished applying icons!\nCheck Editor.log.");
        }
    }
}
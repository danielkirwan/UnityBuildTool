using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            string buildName = BuildNameBox.Text;
            string version = VersionBox.Text;
            string buildType = (BuildTypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "release";

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

            string arguments =
                $"-batchmode -quit " +
                $"-projectPath \"{projectPath}\" " +
                $"-executeMethod BuildAutomation.BuildPC " +
                $"-buildPath=\"{outputPath}\" " +
                $"-buildName=\"{buildName}\" " +
                $"-version=\"{version}\" " +
                $"-buildType=\"{buildType}\"";

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

        /// <summary>
        /// GENERATE ICONS BUTTON:
        /// - Generates Unity icons in:
        ///   Assets/Editor/GeneratedIcons/Standalone
        ///   Assets/Editor/GeneratedIcons/Android
        ///   Assets/Editor/GeneratedIcons/iOS
        /// </summary>
        private void GenerateIcons_Click(object sender, RoutedEventArgs e)
        {
            string src = IconSourcePath.Text;
            string projectPath = ProjectPathBox.Text;
            string extraOutput = IconOutputPath.Text;

            if (!File.Exists(src))
            {
                MessageBox.Show("Please select a valid source image.");
                return;
            }

            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            {
                MessageBox.Show("Please select a valid Unity project path (Project Path).");
                return;
            }

            string generatedRoot = GenerateUnityIconSet(src, projectPath);

            if (!string.IsNullOrWhiteSpace(extraOutput))
            {
                try
                {
                    Directory.CreateDirectory(extraOutput);

                    string standaloneDir = Path.Combine(generatedRoot, "Standalone");
                    string icon1024 = Path.Combine(standaloneDir, "icon_1024.png");
                    string baseForExternal = File.Exists(icon1024) ? icon1024 : src;

                    if (GenWindows.IsChecked == true)
                        GenerateWindowsICO(baseForExternal, Path.Combine(extraOutput, "app_icon.ico"));

                    if (GenMac.IsChecked == true)
                        GenerateMacICNS(baseForExternal, Path.Combine(extraOutput, "app_icon.icns"));

                    if (GenAndroid.IsChecked == true)
                        GenerateAndroidMipmaps(baseForExternal, extraOutput);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error generating external icons:\n" + ex.Message);
                    return;
                }
            }

            MessageBox.Show("Unity icon set generated successfully!\n" +
                            "Assets/Editor/GeneratedIcons has been updated.");
        }

        private string GenerateUnityIconSet(string sourceImagePath, string projectPath)
        {
            string baseDir = Path.Combine(projectPath, "Assets/Editor/GeneratedIcons");
            string standaloneDir = Path.Combine(baseDir, "Standalone");
            string androidDir = Path.Combine(baseDir, "Android");
            string iosDir = Path.Combine(baseDir, "iOS");

            Directory.CreateDirectory(standaloneDir);
            Directory.CreateDirectory(androidDir);
            Directory.CreateDirectory(iosDir);

            using (var fs = new FileStream(sourceImagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var original = System.Drawing.Image.FromStream(fs))
            {
                int[] standaloneSizes = { 16, 32, 48, 64, 128, 256, 512, 1024 };
                foreach (int size in standaloneSizes)
                {
                    string outPath = Path.Combine(standaloneDir, $"icon_{size}.png");
                    SaveResizedImage(original, outPath, size);
                }

                var androidSizes = new Dictionary<string, int>
                {
                    { "android_48", 48 },     
                    { "android_72", 72 },     
                    { "android_96", 96 },     
                    { "android_144", 144 },   
                    { "android_192", 192 }    
                };
                foreach (var kv in androidSizes)
                {
                    string outPath = Path.Combine(androidDir, kv.Key + ".png");
                    SaveResizedImage(original, outPath, kv.Value);
                }

                var iosSizes = new Dictionary<string, int>
                {
                    { "ios_60", 60 },      
                    { "ios_120", 120 },    
                    { "ios_180", 180 },    
                    { "ios_76", 76 },      
                    { "ios_152", 152 },    
                    { "ios_167", 167 },    
                    { "ios_1024", 1024 }   
                };
                foreach (var kv in iosSizes)
                {
                    string outPath = Path.Combine(iosDir, kv.Key + ".png");
                    SaveResizedImage(original, outPath, kv.Value);
                }
            }

            return baseDir;
        }
        private void SaveResizedImage(System.Drawing.Image original, string outputPath, int size)
        {
            using (var square = new System.Drawing.Bitmap(size, size))
            using (var g = System.Drawing.Graphics.FromImage(square))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                float ratio = Math.Min((float)size / original.Width, (float)size / original.Height);
                int newWidth = (int)(original.Width * ratio);
                int newHeight = (int)(original.Height * ratio);

                int x = (size - newWidth) / 2;
                int y = (size - newHeight) / 2;

                g.DrawImage(original, x, y, newWidth, newHeight);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                square.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void GenerateWindowsICO(string src, string output)
        {
            using (var img = System.Drawing.Image.FromFile(src))
            {
                var iconSizes = new[] { 16, 32, 48, 64, 128, 256 };
                using (var fs = new FileStream(output, FileMode.Create))
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write((short)0);
                    bw.Write((short)1);
                    bw.Write((short)iconSizes.Length);

                    long imageDataOffset = 6 + (16 * iconSizes.Length);

                    foreach (var size in iconSizes)
                    {
                        using (var bmp = new System.Drawing.Bitmap(img, new System.Drawing.Size(size, size)))
                        using (var ms = new MemoryStream())
                        {
                            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            byte[] png = ms.ToArray();

                            bw.Write((byte)size);
                            bw.Write((byte)size);
                            bw.Write((byte)0);
                            bw.Write((byte)0);
                            bw.Write((short)1);
                            bw.Write((short)32);
                            bw.Write(png.Length);
                            bw.Write((int)imageDataOffset);

                            imageDataOffset += png.Length;
                        }
                    }

                    foreach (var size in iconSizes)
                    {
                        using (var bmp = new System.Drawing.Bitmap(img, new System.Drawing.Size(size, size)))
                        using (var ms = new MemoryStream())
                        {
                            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            byte[] png = ms.ToArray();
                            bw.Write(png);
                        }
                    }
                }
            }
        }

        private void GenerateMacICNS(string src, string output)
        {
            var sizes = new[] { 16, 32, 64, 128, 256, 512, 1024 };
            using (var fs = new FileStream(output, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(System.Text.Encoding.ASCII.GetBytes("icns"));
                bw.Write(0); 
                long totalSize = 8;

                foreach (var s in sizes)
                {
                    using (var img = new System.Drawing.Bitmap(src))
                    using (var resized = new System.Drawing.Bitmap(img, new System.Drawing.Size(s, s)))
                    using (var ms = new MemoryStream())
                    {
                        resized.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        byte[] png = ms.ToArray();
                        string type = $"ic0{(int)Math.Log(s, 2)}";

                        bw.Write(System.Text.Encoding.ASCII.GetBytes(type));
                        bw.Write(png.Length + 8);
                        bw.Write(png);

                        totalSize += png.Length + 8;
                    }
                }

                fs.Position = 4;
                bw.Write((uint)totalSize);
            }
        }

        private void GenerateAndroidMipmaps(string src, string output)
        {
            var sizes = new Dictionary<string, int>
            {
                { "mipmap-mdpi", 48 },
                { "mipmap-hdpi", 72 },
                { "mipmap-xhdpi", 96 },
                { "mipmap-xxhdpi", 144 },
                { "mipmap-xxxhdpi", 192 }
            };

            foreach (var kv in sizes)
            {
                string folder = Path.Combine(output, kv.Key);
                Directory.CreateDirectory(folder);

                using (var img = new System.Drawing.Bitmap(src))
                using (var resized = new System.Drawing.Bitmap(img, new System.Drawing.Size(kv.Value, kv.Value)))
                {
                    resized.Save(Path.Combine(folder, "app_icon.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        private async void ApplyIconsToUnity_Click(object sender, RoutedEventArgs e)
        {
            string unityVersion = UnityVersionCombo.SelectedItem as string;
            string projectPath = ProjectPathBox.Text;

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

            string arguments =
                $"-batchmode -quit " +
                $"-projectPath \"{projectPath}\" " +
                $"-executeMethod IconAutomation.ApplyIcons " +
                $"-iconsRootPath=\"{iconsRoot}\"";

            MessageBox.Show("Running Unity to apply icons...");

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

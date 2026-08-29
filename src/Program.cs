using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PlasmaOldSchool
{
    internal enum LaunchMode
    {
        Configure,
        FullScreen,
        Preview,
        SelfTest,
        GpuSelfTest,
        Direct3DSelfTest
    }

    internal sealed class LaunchRequest
    {
        public LaunchMode Mode;
        public IntPtr PreviewHandle;
    }

    internal static class Program
    {
        private const string InstallationRegistryPath = @"Software\Valvik\PlasmaOldSchool";
        private static string _runtimeDirectory;

        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                _runtimeDirectory = ResolveRuntimeDirectory();
                NativeMethods.TrySetDllDirectory(_runtimeDirectory);
                NativeMethods.TryEnableDpiAwareness();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                LaunchRequest request = ParseArguments(args);
                if (request.Mode == LaunchMode.SelfTest)
                {
                    RunSelfTest();
                }
                else if (request.Mode == LaunchMode.GpuSelfTest)
                {
                    RunGpuSelfTest();
                }
                else if (request.Mode == LaunchMode.Direct3DSelfTest)
                {
                    RunDirect3DSelfTest();
                }
                else if (request.Mode == LaunchMode.FullScreen)
                {
                    Application.Run(new ScreenSaverContext(IntPtr.Zero));
                }
                else if (request.Mode == LaunchMode.Preview && request.PreviewHandle != IntPtr.Zero)
                {
                    Application.Run(new ScreenSaverContext(request.PreviewHandle));
                }
                else
                {
                    RunConfiguration();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo iniciar Plasma Old School.\r\n\r\n" + ex.Message,
                    "Plasma Old School",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void RunConfiguration()
        {
            string configurationExecutable = Path.Combine(_runtimeDirectory, "PlasmaOldSchool.Config.exe");
            if (File.Exists(configurationExecutable))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(configurationExecutable);
                startInfo.UseShellExecute = true;
                Process.Start(startInfo);
                return;
            }

            MessageBox.Show(
                "No se encontró la aplicación de configuración WinUI 3. Reinstala Plasma Old School para restaurar sus archivos.",
                "Plasma Old School",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static string ResolveRuntimeDirectory()
        {
            string executableDirectory = Path.GetDirectoryName(Application.ExecutablePath) ?? AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(Path.Combine(executableDirectory, "PlasmaOldSchool.Config.exe")) ||
                File.Exists(Path.Combine(executableDirectory, "PlasmaD3D11.dll")))
            {
                return executableDirectory;
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(InstallationRegistryPath))
                {
                    string installedDirectory = key == null ? null : Convert.ToString(key.GetValue("InstallLocation"), CultureInfo.InvariantCulture);
                    if (!String.IsNullOrEmpty(installedDirectory) && Directory.Exists(installedDirectory))
                    {
                        return installedDirectory;
                    }
                }
            }
            catch
            {
                // La copia portátil continúa usando su propia carpeta.
            }
            return executableDirectory;
        }

        private static LaunchRequest ParseArguments(string[] args)
        {
            LaunchRequest request = new LaunchRequest();
            request.Mode = LaunchMode.Configure;

            if (args == null || args.Length == 0)
            {
                return request;
            }

            string first = args[0].Trim().ToLowerInvariant();
            if (first == "/test" || first == "-test")
            {
                request.Mode = LaunchMode.SelfTest;
                return request;
            }

            if (first == "/gputest" || first == "-gputest")
            {
                request.Mode = LaunchMode.GpuSelfTest;
                return request;
            }

            if (first == "/d3dtest" || first == "-d3dtest")
            {
                request.Mode = LaunchMode.Direct3DSelfTest;
                return request;
            }

            if (first.StartsWith("/s") || first.StartsWith("-s"))
            {
                request.Mode = LaunchMode.FullScreen;
                return request;
            }

            if (first.StartsWith("/p") || first.StartsWith("-p"))
            {
                string handleText = String.Empty;
                int separator = first.IndexOf(':');
                if (separator >= 0 && separator + 1 < first.Length)
                {
                    handleText = first.Substring(separator + 1);
                }
                else if (args.Length > 1)
                {
                    handleText = args[1];
                }

                long handleValue;
                if (Int64.TryParse(handleText, NumberStyles.Integer, CultureInfo.InvariantCulture, out handleValue))
                {
                    request.Mode = LaunchMode.Preview;
                    request.PreviewHandle = new IntPtr(handleValue);
                }
                return request;
            }

            return request;
        }

        private static void RunSelfTest()
        {
            int quality;
            int paletteIndex;
            for (quality = 1; quality <= 3; quality++)
            {
                for (paletteIndex = 0; paletteIndex < PaletteCatalog.Presets.Length - 1; paletteIndex++)
                {
                    PlasmaSettings testSettings = new PlasmaSettings();
                    testSettings.RenderQuality = quality;
                    testSettings.PaletteKey = PaletteCatalog.Presets[paletteIndex].Key;
                    testSettings.Colors = PaletteCatalog.CopyColors(testSettings.PaletteKey);
                    using (PlasmaEngine engine = new PlasmaEngine(testSettings))
                    {
                        int initial = engine.Frame.GetPixel(engine.Width / 2, engine.Height / 2).ToArgb();
                        int frame;
                        for (frame = 0; frame < 8; frame++)
                        {
                            engine.Advance(0.02);
                        }

                        int changedSamples = 0;
                        int y;
                        int x;
                        for (y = engine.Height / 12; y < engine.Height; y += Math.Max(1, engine.Height / 6))
                        {
                            for (x = engine.Width / 12; x < engine.Width; x += Math.Max(1, engine.Width / 8))
                            {
                                if (engine.Frame.GetPixel(x, y).ToArgb() != initial)
                                {
                                    changedSamples++;
                                }
                            }
                        }

                        if (changedSamples < 8)
                        {
                            Environment.ExitCode = 2;
                            return;
                        }
                    }
                }
            }
            VerifyCpuPixelation();
            if (!VerifySettingsValidationAndPersistence())
            {
                Environment.ExitCode = 5;
                return;
            }
            Environment.ExitCode = 0;
        }

        private static bool VerifySettingsValidationAndPersistence()
        {
            string testPath = @"Software\PlasmaOldSchoolScreenSaver\Tests\" + Guid.NewGuid().ToString("N");
            try
            {
                PlasmaSettings saved = new PlasmaSettings();
                saved.PaletteKey = "custom";
                saved.Colors = new[] { System.Drawing.Color.Black, System.Drawing.Color.Red, System.Drawing.Color.Lime, System.Drawing.Color.Blue };
                saved.MotionSpeed = -9.0;
                saved.TargetFps = 999;
                saved.Pixelation = -4;
                saved.Validate();
                if (saved.MotionSpeed != 0.01 || saved.TargetFps != 75 || saved.Pixelation != 1)
                {
                    return false;
                }

                saved.MotionSpeed = 0.08;
                saved.TargetFps = 40;
                saved.Pixelation = 6;
                saved.SaveToRegistryPath(testPath);
                PlasmaSettings loaded = PlasmaSettings.LoadFromRegistryPath(testPath);
                return loaded.PaletteKey == "custom" &&
                    Math.Abs(loaded.MotionSpeed - 0.08) < 0.0001 &&
                    loaded.TargetFps == 40 && loaded.Pixelation == 6 &&
                    loaded.Colors[2].ToArgb() == System.Drawing.Color.Lime.ToArgb();
            }
            catch
            {
                return false;
            }
            finally
            {
                try { Registry.CurrentUser.DeleteSubKeyTree(testPath, false); } catch { }
            }
        }

        private static void VerifyCpuPixelation()
        {
            PlasmaSettings testSettings = new PlasmaSettings();
            testSettings.RenderQuality = 1;
            testSettings.Pixelation = 4;
            testSettings.MovingOrigin = false;
            testSettings.ColorCycle = false;
            using (PlasmaEngine engine = new PlasmaEngine(testSettings))
            {
                int blockY;
                int blockX;
                for (blockY = 0; blockY + testSettings.Pixelation <= engine.Height; blockY += testSettings.Pixelation)
                {
                    for (blockX = 0; blockX + testSettings.Pixelation <= engine.Width; blockX += testSettings.Pixelation)
                    {
                        int expected = engine.Frame.GetPixel(blockX, blockY).ToArgb();
                        int y;
                        for (y = blockY; y < blockY + testSettings.Pixelation; y++)
                        {
                            int x;
                            for (x = blockX; x < blockX + testSettings.Pixelation; x++)
                            {
                                if (engine.Frame.GetPixel(x, y).ToArgb() != expected)
                                {
                                    Environment.ExitCode = 2;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void RunGpuSelfTest()
        {
            try
            {
                using (Form host = new Form())
                {
                    host.ShowInTaskbar = false;
                    host.CreateControl();
                    using (PlasmaEngine engine = new PlasmaEngine(new PlasmaSettings()))
                    using (GpuPlasmaRenderer renderer = new GpuPlasmaRenderer(host))
                    {
                        engine.EnableGpuMode();
                        renderer.Render(engine, 320, 180);
                    }
                }
                Environment.ExitCode = 0;
            }
            catch
            {
                Environment.ExitCode = 3;
            }
        }

        private static void RunDirect3DSelfTest()
        {
            try
            {
                using (Form host = new Form())
                {
                    host.ShowInTaskbar = false;
                    host.ClientSize = new System.Drawing.Size(320, 180);
                    host.CreateControl();
                    using (PlasmaEngine engine = new PlasmaEngine(new PlasmaSettings()))
                    using (Direct3DPlasmaRenderer renderer = new Direct3DPlasmaRenderer(host))
                    {
                        engine.EnableGpuMode();
                        if (!renderer.Render(engine, 320, 180))
                        {
                            Environment.ExitCode = 4;
                            return;
                        }
                    }
                }
                Environment.ExitCode = 0;
            }
            catch
            {
                Environment.ExitCode = 4;
            }
        }
    }
}

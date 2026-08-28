using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;

namespace PlasmaOldSchool.Config
{
    public sealed partial class MainWindow : Window
    {
        private const string Path = @"Software\PlasmaOldSchoolScreenSaver";
        private readonly Dictionary<string, Control> _controls = new Dictionary<string, Control>();

        public MainWindow()
        {
            InitializeComponent();
            Language.SelectedIndex = String.Equals(Read("Language", "es"), "en", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            Translate();
            ShowPage("general");
        }

        private void NavigationChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            NavigationViewItem item = args.SelectedItem as NavigationViewItem;
            if (item != null) ShowPage((string)item.Tag);
        }

        private void ShowPage(string page)
        {
            _controls.Clear();
            StackPanel panel = new StackPanel { Spacing = 14, Padding = new Thickness(28, 12, 28, 12) };
            if (page == "general")
            {
                AddCombo(panel, "Palette", L("palette"), new[] { "amiga", "ice", "copper", "acid", "rgb", "fire", "forest", "ocean", "violet", "terminal", "mono", "custom" });
                AddSlider(panel, "MotionSpeed", L("motion_speed"), 15, 250, 100);
                AddSlider(panel, "SpatialScale", L("spatial_scale"), 45, 220, 100);
                AddSlider(panel, "Warp", L("warp"), 0, 100, 65);
                AddSlider(panel, "Pixelation", L("pixelation"), 1, 20, 1);
                AddToggle(panel, "ColorCycle", L("color_cycle"), true);
            }
            else if (page == "display")
            {
                AddCombo(panel, "RenderQuality", L("quality"), new[] { "1", "2", "3" });
                AddSlider(panel, "TargetFps", L("target_fps"), 20, 75, 50);
                AddToggle(panel, "PowerSaving", L("power_saving"), true);
                AddToggle(panel, "Scanlines", L("scanlines"), true);
                AddSlider(panel, "ScanlineSpacing", L("line_spacing"), 2, 8, 4);
                AddSlider(panel, "ScanlineOpacity", L("line_opacity"), 0, 100, 42);
                AddToggle(panel, "Vignette", L("vignette"), true);
            }
            else
            {
                AddSlider(panel, "WaveDensity", L("density"), 50, 200, 100);
                AddSlider(panel, "RadialPulse", L("pulse"), 0, 200, 100);
                AddSlider(panel, "RotationSpeed", L("rotation"), 0, 200, 44);
                AddSlider(panel, "Brightness", L("brightness"), 50, 160, 100);
                AddSlider(panel, "Contrast", L("contrast"), 50, 200, 100);
                AddToggle(panel, "RandomOrigin", L("random_origin"), true);
                AddToggle(panel, "MovingOrigin", L("moving_origin"), true);
            }
            ContentFrame.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private void AddSlider(Panel panel, string key, string label, int min, int max, int fallback)
        {
            StackPanel item = new StackPanel(); item.Children.Add(new TextBlock { Text = label });
            Slider slider = new Slider { Minimum = min, Maximum = max, Value = ReadInt(key, fallback), StepFrequency = 1, IsThumbToolTipEnabled = true };
            item.Children.Add(slider); panel.Children.Add(item); _controls[key] = slider;
        }

        private void AddToggle(Panel panel, string key, string label, bool fallback)
        {
            ToggleSwitch toggle = new ToggleSwitch { Header = label, IsOn = ReadInt(key, fallback ? 1 : 0) != 0 };
            panel.Children.Add(toggle); _controls[key] = toggle;
        }

        private void AddCombo(Panel panel, string key, string label, string[] values)
        {
            StackPanel item = new StackPanel(); item.Children.Add(new TextBlock { Text = label });
            ComboBox combo = new ComboBox(); foreach (string value in values) combo.Items.Add(value);
            string saved = Read(key, values[0]); int index = Array.IndexOf(values, saved); combo.SelectedIndex = index < 0 ? 0 : index;
            item.Children.Add(combo); panel.Children.Add(item); _controls[key] = combo;
        }

        private void SaveClicked(object sender, RoutedEventArgs e)
        {
            PersistSettings();
            Close();
        }

        private void PersistSettings()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(Path))
            {
                key.SetValue("Language", Language.SelectedIndex == 1 ? "en" : "es");
                foreach (KeyValuePair<string, Control> entry in _controls)
                {
                    Slider slider = entry.Value as Slider; ToggleSwitch toggle = entry.Value as ToggleSwitch; ComboBox combo = entry.Value as ComboBox;
                    if (slider != null) key.SetValue(entry.Key, ((int)Math.Round(slider.Value)).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    else if (toggle != null) key.SetValue(entry.Key, toggle.IsOn ? 1 : 0, RegistryValueKind.DWord);
                    else if (combo != null) key.SetValue(entry.Key, Convert.ToString(combo.SelectedItem));
                }
            }
        }

        private void TestClicked(object sender, RoutedEventArgs e)
        {
            PersistSettings();
            string screensaver = System.IO.Path.Combine(AppContext.BaseDirectory, "PlasmaOldSchool.scr");
            if (File.Exists(screensaver))
            {
                Process.Start(new ProcessStartInfo(screensaver, "/s") { UseShellExecute = true, WorkingDirectory = AppContext.BaseDirectory });
            }
        }
        private void CancelClicked(object sender, RoutedEventArgs e) { Close(); }
        private void LanguageChanged(object sender, SelectionChangedEventArgs e) { Translate(); ShowPage("general"); }
        private void Translate()
        {
            Subtitle.Text = L("subtitle"); LanguageLabel.Text = L("language"); GeneralItem.Content = L("tab_general"); DisplayItem.Content = L("tab_display"); EngineItem.Content = L("tab_engine"); SaveButton.Content = L("save"); CancelButton.Content = L("cancel"); TestButton.Content = L("test");
        }
        private string L(string key)
        {
            bool en = Language != null && Language.SelectedIndex == 1;
            if (en) { if (key == "subtitle") return "RETRO SCREENSAVER · SETTINGS"; if (key == "language") return "Language"; if (key == "tab_general") return "General"; if (key == "tab_display") return "Display"; if (key == "tab_engine") return "Engine"; if (key == "save") return "Save"; if (key == "cancel") return "Cancel"; if (key == "test") return "Test"; if (key == "palette") return "Palette"; if (key == "motion_speed") return "Speed"; if (key == "spatial_scale") return "Spatial scale"; if (key == "pixelation") return "Pixelation"; if (key == "color_cycle") return "Colour cycling"; if (key == "quality") return "Quality"; if (key == "target_fps") return "Target FPS"; if (key == "power_saving") return "Power saving"; if (key == "scanlines") return "CRT scanlines"; if (key == "line_spacing") return "CRT spacing"; if (key == "line_opacity") return "CRT opacity"; if (key == "vignette") return "CRT vignette"; if (key == "density") return "Wave density"; if (key == "pulse") return "Radial pulse"; if (key == "rotation") return "Rotation"; if (key == "brightness") return "Brightness"; if (key == "contrast") return "Contrast"; if (key == "random_origin") return "Random origin"; if (key == "moving_origin") return "Moving origin"; }
            if (key == "subtitle") return "SALVAPANTALLAS RETRO · CONFIGURACIÓN"; if (key == "language") return "Idioma"; if (key == "tab_general") return "General"; if (key == "tab_display") return "Pantalla"; if (key == "tab_engine") return "Motor"; if (key == "save") return "Guardar"; if (key == "cancel") return "Cancelar"; if (key == "test") return "Probar"; if (key == "palette") return "Paleta"; if (key == "motion_speed") return "Velocidad"; if (key == "spatial_scale") return "Escala espacial"; if (key == "pixelation") return "Pixelado"; if (key == "color_cycle") return "Evolución cromática"; if (key == "quality") return "Calidad"; if (key == "target_fps") return "FPS objetivo"; if (key == "power_saving") return "Ahorro de energía"; if (key == "scanlines") return "Scanlines CRT"; if (key == "line_spacing") return "Separación CRT"; if (key == "line_opacity") return "Opacidad CRT"; if (key == "vignette") return "Viñeta CRT"; if (key == "density") return "Densidad de ondas"; if (key == "pulse") return "Pulso radial"; if (key == "rotation") return "Giro"; if (key == "brightness") return "Brillo"; if (key == "contrast") return "Contraste"; if (key == "random_origin") return "Origen aleatorio"; if (key == "moving_origin") return "Órbita móvil"; return key;
        }
        private static string Read(string name, string fallback) { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Path)) return k == null ? fallback : Convert.ToString(k.GetValue(name, fallback)); }
        private static int ReadInt(string name, int fallback) { int value; return Int32.TryParse(Read(name, fallback.ToString()), out value) ? value : fallback; }
    }
}

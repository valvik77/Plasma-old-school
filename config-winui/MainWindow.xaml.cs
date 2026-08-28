using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;

namespace PlasmaOldSchool.Config
{
    public sealed partial class MainWindow : Window
    {
        private const string Path = @"Software\PlasmaOldSchoolScreenSaver";
        private readonly Dictionary<string, Control> _controls = new Dictionary<string, Control>();
        private readonly Dictionary<string, string> _pendingValues = new Dictionary<string, string>();
        private readonly bool _legacyPercentageValues;
        private StackPanel _customColorsPanel;

        public MainWindow()
        {
            InitializeComponent();
            _legacyPercentageValues = UsesLegacyPercentageValues();
            if (_legacyPercentageValues) PrepareLegacyMigration();
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
                AddCustomColors(panel);
                AddSlider(panel, "MotionSpeed", L("motion_speed"), 1, 150, 0.35, 100);
                AddSlider(panel, "SpatialScale", L("spatial_scale"), 45, 220, 1.0, 100);
                AddSlider(panel, "Warp", L("warp"), 0, 100, 0.65, 100);
                AddSlider(panel, "Pixelation", L("pixelation"), 1, 20, 1.0);
                AddToggle(panel, "ColorCycle", L("color_cycle"), true);
            }
            else if (page == "display")
            {
                AddCombo(panel, "RenderQuality", L("quality"), new[] { "1", "2", "3" });
                AddSlider(panel, "TargetFps", L("target_fps"), 20, 75, 50.0);
                AddToggle(panel, "PowerSaving", L("power_saving"), true);
                AddToggle(panel, "Scanlines", L("scanlines"), true);
                AddSlider(panel, "ScanlineSpacing", L("line_spacing"), 2, 8, 4.0);
                AddSlider(panel, "ScanlineOpacity", L("line_opacity"), 0, 100, 42.0);
                AddToggle(panel, "Vignette", L("vignette"), true);
            }
            else
            {
                AddSlider(panel, "WaveDensity", L("density"), 50, 200, 1.0, 100);
                AddSlider(panel, "RadialPulse", L("pulse"), 0, 200, 1.0, 100);
                AddSlider(panel, "RotationSpeed", L("rotation"), 0, 200, 0.44, 100);
                AddSlider(panel, "Brightness", L("brightness"), 50, 160, 1.0, 100);
                AddSlider(panel, "Contrast", L("contrast"), 50, 200, 1.0, 100);
                AddToggle(panel, "RandomOrigin", L("random_origin"), true);
                AddToggle(panel, "MovingOrigin", L("moving_origin"), true);
            }
            ContentFrame.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private void AddSlider(Panel panel, string key, string label, int min, int max, double fallback, double divisor = 1.0)
        {
            StackPanel item = new StackPanel();
            Grid header = new Grid(); header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            TextBlock valueLabel = new TextBlock { Opacity = 0.7 };
            header.Children.Add(new TextBlock { Text = label }); Grid.SetColumn(valueLabel, 1); header.Children.Add(valueLabel); item.Children.Add(header);
            double saved = ReadDoubleValue(key, fallback);
            double sliderValue = Math.Max(min, Math.Min(max, saved * divisor));
            Slider slider = new Slider { Minimum = min, Maximum = max, Value = sliderValue, StepFrequency = 1, IsThumbToolTipEnabled = true };
            Action update = () =>
            {
                double actual = slider.Value / divisor;
                _pendingValues[key] = actual.ToString(divisor == 1.0 ? "0" : "0.00", CultureInfo.InvariantCulture);
                valueLabel.Text = divisor == 1.0 ? Math.Round(actual).ToString(CultureInfo.CurrentCulture) : actual.ToString("0.00", CultureInfo.CurrentCulture) + "×";
            };
            slider.ValueChanged += (sender, args) => update(); update();
            item.Children.Add(slider); panel.Children.Add(item); _controls[key] = slider;
        }

        private void AddToggle(Panel panel, string key, string label, bool fallback)
        {
            ToggleSwitch toggle = new ToggleSwitch { Header = label, IsOn = ReadIntValue(key, fallback ? 1 : 0) != 0 };
            toggle.Toggled += (sender, args) => _pendingValues[key] = toggle.IsOn ? "1" : "0";
            _pendingValues[key] = toggle.IsOn ? "1" : "0";
            panel.Children.Add(toggle); _controls[key] = toggle;
        }

        private void AddCombo(Panel panel, string key, string label, string[] values)
        {
            StackPanel item = new StackPanel(); item.Children.Add(new TextBlock { Text = label });
            ComboBox combo = new ComboBox(); foreach (string value in values) combo.Items.Add(value);
            string saved = CurrentValue(key, values[0]); int index = Array.IndexOf(values, saved); combo.SelectedIndex = index < 0 ? 0 : index;
            combo.SelectionChanged += (sender, args) =>
            {
                _pendingValues[key] = Convert.ToString(combo.SelectedItem, CultureInfo.InvariantCulture);
                if (key == "Palette" && _customColorsPanel != null) _customColorsPanel.Visibility = combo.SelectedIndex == values.Length - 1 ? Visibility.Visible : Visibility.Collapsed;
            };
            _pendingValues[key] = Convert.ToString(combo.SelectedItem, CultureInfo.InvariantCulture);
            item.Children.Add(combo); panel.Children.Add(item); _controls[key] = combo;
        }

        private void AddCustomColors(Panel panel)
        {
            _customColorsPanel = new StackPanel { Spacing = 6 };
            _customColorsPanel.Children.Add(new TextBlock { Text = L("custom_colors") });
            StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            string[] defaults = { "FFFF146F", "FFFFCC33", "FF00E5FF", "FF12002F" };
            for (int index = 0; index < 4; index++)
            {
                string key = "Color" + (index + 1).ToString(CultureInfo.InvariantCulture);
                Windows.UI.Color color = ParseColor(CurrentValue(key, defaults[index]));
                Button swatch = new Button { Width = 118, Height = 40 };
                ColorPicker picker = new ColorPicker { Color = color, IsAlphaEnabled = false, IsAlphaSliderVisible = false, IsAlphaTextInputVisible = false };
                Flyout flyout = new Flyout { Content = picker }; swatch.Flyout = flyout;
                Action update = () =>
                {
                    Windows.UI.Color selected = picker.Color;
                    string hex = selected.A.ToString("X2") + selected.R.ToString("X2") + selected.G.ToString("X2") + selected.B.ToString("X2");
                    _pendingValues[key] = hex; swatch.Content = "#" + hex.Substring(2); swatch.Background = new SolidColorBrush(selected);
                };
                picker.ColorChanged += (sender, args) => update(); update(); row.Children.Add(swatch);
            }
            _customColorsPanel.Children.Add(row);
            _customColorsPanel.Visibility = String.Equals(CurrentValue("Palette", "amiga"), "custom", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
            panel.Children.Add(_customColorsPanel);
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
                foreach (KeyValuePair<string, string> entry in _pendingValues)
                {
                    key.SetValue(entry.Key, entry.Value, RegistryValueKind.String);
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
            if (en) { if (key == "subtitle") return "RETRO SCREENSAVER · SETTINGS"; if (key == "language") return "Language"; if (key == "tab_general") return "General"; if (key == "tab_display") return "Display"; if (key == "tab_engine") return "Engine"; if (key == "save") return "Save"; if (key == "cancel") return "Cancel"; if (key == "test") return "Test"; if (key == "palette") return "Palette"; if (key == "custom_colors") return "Custom colours"; if (key == "motion_speed") return "Speed"; if (key == "spatial_scale") return "Zoom / spatial scale"; if (key == "pixelation") return "Pixelation"; if (key == "color_cycle") return "Colour cycling"; if (key == "quality") return "Quality"; if (key == "target_fps") return "Target FPS"; if (key == "power_saving") return "Power saving"; if (key == "scanlines") return "CRT scanlines"; if (key == "line_spacing") return "CRT spacing"; if (key == "line_opacity") return "CRT opacity"; if (key == "vignette") return "CRT vignette"; if (key == "density") return "Wave density"; if (key == "pulse") return "Radial pulse"; if (key == "rotation") return "Rotation"; if (key == "brightness") return "Brightness"; if (key == "contrast") return "Contrast"; if (key == "random_origin") return "Random origin"; if (key == "moving_origin") return "Moving origin"; }
            if (key == "subtitle") return "SALVAPANTALLAS RETRO · CONFIGURACIÓN"; if (key == "language") return "Idioma"; if (key == "tab_general") return "General"; if (key == "tab_display") return "Pantalla"; if (key == "tab_engine") return "Motor"; if (key == "save") return "Guardar"; if (key == "cancel") return "Cancelar"; if (key == "test") return "Probar"; if (key == "palette") return "Paleta"; if (key == "custom_colors") return "Colores personalizados"; if (key == "motion_speed") return "Velocidad"; if (key == "spatial_scale") return "Zoom / escala espacial"; if (key == "pixelation") return "Pixelado"; if (key == "color_cycle") return "Evolución cromática"; if (key == "quality") return "Calidad"; if (key == "target_fps") return "FPS objetivo"; if (key == "power_saving") return "Ahorro de energía"; if (key == "scanlines") return "Scanlines CRT"; if (key == "line_spacing") return "Separación CRT"; if (key == "line_opacity") return "Opacidad CRT"; if (key == "vignette") return "Viñeta CRT"; if (key == "density") return "Densidad de ondas"; if (key == "pulse") return "Pulso radial"; if (key == "rotation") return "Giro"; if (key == "brightness") return "Brillo"; if (key == "contrast") return "Contraste"; if (key == "random_origin") return "Origen aleatorio"; if (key == "moving_origin") return "Órbita móvil"; return key;
        }
        private static string Read(string name, string fallback) { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Path)) return k == null ? fallback : Convert.ToString(k.GetValue(name, fallback)); }
        private static int ReadInt(string name, int fallback) { int value; return Int32.TryParse(Read(name, fallback.ToString()), out value) ? value : fallback; }
        private string CurrentValue(string name, string fallback) { string value; if (!_pendingValues.TryGetValue(name, out value)) { value = Read(name, fallback); _pendingValues[name] = value; } return value; }
        private int ReadIntValue(string name, int fallback) { int value; return Int32.TryParse(CurrentValue(name, fallback.ToString(CultureInfo.InvariantCulture)), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : fallback; }
        private double ReadDoubleValue(string name, double fallback) { double value; return Double.TryParse(CurrentValue(name, fallback.ToString(CultureInfo.InvariantCulture)), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : fallback; }
        private static Windows.UI.Color ParseColor(string value) { uint argb; if (!UInt32.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out argb)) argb = 0xFFFFFFFF; return Windows.UI.Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb); }
        private static bool UsesLegacyPercentageValues() { return ReadDouble("MotionSpeed", 0.35) > 2.5 || ReadDouble("SpatialScale", 1.0) > 3.0 || ReadDouble("Warp", 0.65) > 1.0 || ReadDouble("WaveDensity", 1.0) > 2.0 || ReadDouble("RadialPulse", 1.0) > 2.0 || ReadDouble("RotationSpeed", 0.44) > 2.0 || ReadDouble("Brightness", 1.0) > 1.6 || ReadDouble("Contrast", 1.0) > 2.0; }
        private static double ReadDouble(string name, double fallback) { double value; return Double.TryParse(Read(name, fallback.ToString(CultureInfo.InvariantCulture)), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : fallback; }
        private void PrepareLegacyMigration()
        {
            string[] names = { "MotionSpeed", "SpatialScale", "Warp", "WaveDensity", "RadialPulse", "RotationSpeed", "Brightness", "Contrast" };
            double[] fallbacks = { 35, 100, 65, 100, 100, 44, 100, 100 };
            for (int index = 0; index < names.Length; index++)
            {
                _pendingValues[names[index]] = (ReadDouble(names[index], fallbacks[index]) / 100.0).ToString("0.00", CultureInfo.InvariantCulture);
            }
        }
    }
}

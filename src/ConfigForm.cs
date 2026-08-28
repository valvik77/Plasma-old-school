using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PlasmaOldSchool
{
    // Diseño Fluent inspirado en WinUI, sin introducir dependencias en el .scr.
    internal sealed class ConfigForm : Form
    {
        private readonly PlasmaSettings _settings;
        private readonly Dictionary<Control, string> _texts = new Dictionary<Control, string>();
        private readonly List<Panel> _cards = new List<Panel>();
        private readonly TabControl _pages;
        private readonly TabPage _generalTab, _displayTab, _engineTab;
        private readonly FlowLayoutPanel _generalStack, _displayStack, _engineStack;
        private readonly ComboBox _language, _palette, _quality;
        private readonly Button[] _colors = new Button[4];
        private readonly CheckBox _cycle, _scanlines, _mirrorH, _mirrorV, _vignette, _saving, _random, _moving;
        private readonly TrackBar _colorSpeed, _motion, _scale, _warp, _pixelation, _density, _pulse, _rotation, _brightness, _contrast, _fps, _lineSpacing, _lineOpacity, _originX, _originY;
        private readonly Label _colorSpeedValue, _motionValue, _scaleValue, _warpValue, _pixelationValue, _densityValue, _pulseValue, _rotationValue, _brightnessValue, _contrastValue, _fpsValue, _lineSpacingValue, _lineOpacityValue, _originXValue, _originYValue;
        private bool _loading;
        private readonly bool _dark;

        internal ConfigForm()
        {
            _loading = true; _settings = PlasmaSettings.Load(); _dark = IsDark();
            AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(1120, 900); MinimumSize = new Size(960, 760);
            FormBorderStyle = FormBorderStyle.Sizable; StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f); Text = "Plasma Old School";

            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24, 20, 24, 16), ColumnCount = 1, RowCount = 3 };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); Controls.Add(root);
            root.Controls.Add(Header(out _language), 0, 0);
            _pages = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 6) };
            _generalTab = new TabPage(); _displayTab = new TabPage(); _engineTab = new TabPage();
            _generalStack = CreatePage(_generalTab); _displayStack = CreatePage(_displayTab); _engineStack = CreatePage(_engineTab);
            _pages.TabPages.Add(_generalTab); _pages.TabPages.Add(_displayTab); _pages.TabPages.Add(_engineTab);
            _pages.SizeChanged += delegate { ResizeCards(); }; root.Controls.Add(_pages, 0, 1);

            TableLayoutPanel palette = Card(_generalStack, "palette_section");
            _palette = Combo(); foreach (PaletteDefinition preset in PaletteCatalog.Presets) _palette.Items.Add(preset); _palette.SelectedIndexChanged += OnPalette;
            Row(palette, "palette", _palette);
            FlowLayoutPanel swatches = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
            for (int i = 0; i < 4; i++) { Button b = Button((i + 1).ToString(), false); b.Width = 52; b.Tag = i; b.Click += PickColor; _colors[i] = b; swatches.Controls.Add(b); }
            Row(palette, "colors", swatches);
            _cycle = Check("color_cycle"); _cycle.CheckedChanged += delegate { _colorSpeed.Enabled = _cycle.Checked; }; Row(palette, "", _cycle);
            _colorSpeed = Track(10, 300, 5); _colorSpeedValue = Value(); Slider(palette, "color_speed", _colorSpeed, _colorSpeedValue);

            TableLayoutPanel motionCard = Card(_generalStack, "motion_section");
            _motion = Track(15, 250, 5); _motionValue = Value(); Slider(motionCard, "motion_speed", _motion, _motionValue);
            _scale = Track(45, 220, 5); _scaleValue = Value(); Slider(motionCard, "spatial_scale", _scale, _scaleValue);
            _warp = Track(0, 100, 5); _warpValue = Value(); Slider(motionCard, "warp", _warp, _warpValue);
            _pixelation = Track(1, 20, 1); _pixelationValue = Value(); Slider(motionCard, "pixelation", _pixelation, _pixelationValue);
            _scanlines = Check("scanlines"); Row(motionCard, "", _scanlines);

            TableLayoutPanel render = Card(_displayStack, "render_section");
            _quality = Combo(); Row(render, "quality", _quality);
            _fps = Track(20, 75, 5); _fpsValue = Value(); Slider(render, "target_fps", _fps, _fpsValue);
            _saving = Check("power_saving"); Row(render, "", _saving);
            _lineSpacing = Track(2, 8, 1); _lineSpacingValue = Value(); Slider(render, "line_spacing", _lineSpacing, _lineSpacingValue);
            _lineOpacity = Track(0, 100, 5); _lineOpacityValue = Value(); Slider(render, "line_opacity", _lineOpacity, _lineOpacityValue);
            FlowLayoutPanel visual = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
            _mirrorH = Check("mirror_h"); _mirrorV = Check("mirror_v"); _vignette = Check("vignette"); visual.Controls.Add(_mirrorH); visual.Controls.Add(_mirrorV); visual.Controls.Add(_vignette); Row(render, "", visual);

            TableLayoutPanel engine = Card(_engineStack, "engine_section");
            _density = Track(50, 200, 5); _densityValue = Value(); Slider(engine, "density", _density, _densityValue);
            _pulse = Track(0, 200, 5); _pulseValue = Value(); Slider(engine, "pulse", _pulse, _pulseValue);
            _rotation = Track(0, 200, 5); _rotationValue = Value(); Slider(engine, "rotation", _rotation, _rotationValue);
            _brightness = Track(50, 160, 5); _brightnessValue = Value(); Slider(engine, "brightness", _brightness, _brightnessValue);
            _contrast = Track(50, 200, 5); _contrastValue = Value(); Slider(engine, "contrast", _contrast, _contrastValue);

            TableLayoutPanel origin = Card(_displayStack, "origin_section");
            FlowLayoutPanel originOptions = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
            _random = Check("random_origin"); _moving = Check("moving_origin"); originOptions.Controls.Add(_random); originOptions.Controls.Add(_moving); Row(origin, "", originOptions);
            _originX = Track(10, 90, 5); _originXValue = Value(); Slider(origin, "origin_x", _originX, _originXValue);
            _originY = Track(10, 90, 5); _originYValue = Value(); Slider(origin, "origin_y", _originY, _originYValue);

            foreach (TrackBar track in new[] { _colorSpeed, _motion, _scale, _warp, _pixelation, _density, _pulse, _rotation, _brightness, _contrast, _fps, _lineSpacing, _lineOpacity, _originX, _originY }) track.ValueChanged += delegate { Values(); };
            _saving.CheckedChanged += delegate { Values(); }; _random.CheckedChanged += delegate { _originX.Enabled = _originY.Enabled = !_random.Checked; };

            FlowLayoutPanel footer = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = true };
            Button save = Button("save", true); save.Click += Save; footer.Controls.Add(save); AcceptButton = save;
            Button cancel = Button("cancel", false); cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); }; footer.Controls.Add(cancel); CancelButton = cancel;
            Button test = Button("test", false); test.Click += Test; footer.Controls.Add(test); root.Controls.Add(footer, 0, 2);

            LoadControls(); _loading = false; Translate(); Values(); Theme();
        }

        private Control Header(out ComboBox language)
        {
            TableLayoutPanel header = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2 };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            Label title = new Label { Text = "PLASMA OLD SCHOOL", Font = new Font("Segoe UI Semibold", 20f), AutoSize = true };
            Label subtitle = Label("subtitle"); subtitle.AutoSize = true; subtitle.Top = title.Bottom + 2;
            Panel text = new Panel { AutoSize = true }; text.Controls.Add(title); text.Controls.Add(subtitle);
            language = Combo(); language.Width = 125; language.Items.Add("Español"); language.Items.Add("English"); language.SelectedIndexChanged += LanguageChanged;
            FlowLayoutPanel picker = new FlowLayoutPanel { AutoSize = true }; picker.Controls.Add(Label("language")); picker.Controls.Add(language);
            header.Controls.Add(text, 0, 0); header.Controls.Add(picker, 1, 0); return header;
        }

        private static FlowLayoutPanel CreatePage(TabPage page)
        {
            FlowLayoutPanel stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = false, Padding = new Padding(10) };
            page.Controls.Add(stack);
            return stack;
        }

        private TableLayoutPanel Card(FlowLayoutPanel host, string title)
        {
            Panel card = new Panel { BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18, 14, 18, 14), Margin = new Padding(0, 0, 0, 12), AutoSize = false, Width = 900, Height = 80 };
            Label heading = Label(title); heading.Font = new Font("Segoe UI Semibold", 11f); heading.AutoSize = true; heading.Location = new Point(18, 14);
            TableLayoutPanel table = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Padding = new Padding(0, 8, 0, 0), Location = new Point(18, 40), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); card.Controls.Add(heading); card.Controls.Add(table);
            Action updateHeight = delegate { card.Height = Math.Max(80, table.Bottom + card.Padding.Bottom); };
            card.SizeChanged += delegate { table.Width = Math.Max(1, card.ClientSize.Width - card.Padding.Horizontal); };
            table.SizeChanged += delegate { updateHeight(); };
            host.Controls.Add(card); _cards.Add(card); return table;
        }

        private void ResizeCards()
        {
            int width = Math.Max(620, _pages.ClientSize.Width - 32);
            foreach (Panel card in _cards) card.Width = width;
        }

        private void Row(TableLayoutPanel parent, string label, Control control)
        {
            TableLayoutPanel row = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            if (label.Length > 0) { Label l = Label(label); l.Dock = DockStyle.Fill; l.TextAlign = ContentAlignment.MiddleLeft; row.Controls.Add(l, 0, 0); }
            control.Dock = DockStyle.Fill; control.Margin = new Padding(0, 3, 0, 5); row.Controls.Add(control, 1, 0); parent.Controls.Add(row);
        }

        private void Slider(TableLayoutPanel parent, string label, TrackBar track, Label value)
        {
            TableLayoutPanel row = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 3 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            Label l = Label(label); l.Dock = DockStyle.Fill; l.TextAlign = ContentAlignment.MiddleLeft; track.Dock = DockStyle.Fill; value.Dock = DockStyle.Fill; value.TextAlign = ContentAlignment.MiddleRight;
            row.Controls.Add(l, 0, 0); row.Controls.Add(track, 1, 0); row.Controls.Add(value, 2, 0); parent.Controls.Add(row);
        }

        private ComboBox Combo() { return new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Height = 32 }; }
        private CheckBox Check(string text) { CheckBox check = new CheckBox { AutoSize = true, MinimumSize = new Size(0, 32) }; Localize(check, text); return check; }
        private TrackBar Track(int min, int max, int step) { return new TrackBar { AutoSize = false, Minimum = min, Maximum = max, SmallChange = step, LargeChange = step * 4, TickFrequency = Math.Max(1, (max - min) / 8), Height = 36 }; }
        private Label Value() { return new Label { MinimumSize = new Size(70, 36) }; }
        private Label Label(string text) { Label label = new Label(); Localize(label, text); return label; }
        private Button Button(string text, bool accent) { Button button = new Button { AutoSize = false, Size = new Size(112, 36), Margin = new Padding(8, 8, 0, 0), FlatStyle = FlatStyle.Flat, Tag = accent ? "accent" : "button" }; int number; if (Int32.TryParse(text, out number)) button.Text = text; else Localize(button, text); return button; }

        private void Localize(Control control, string key) { _texts[control] = key; control.Text = T(key); }
        private void LanguageChanged(object sender, EventArgs e) { if (!_loading) { _settings.Language = _language.SelectedIndex == 1 ? "en" : "es"; Translate(); } }
        private void Translate()
        {
            Text = "Plasma Old School — " + T("settings");
            _generalTab.Text = T("tab_general"); _displayTab.Text = T("tab_display"); _engineTab.Text = T("tab_engine");
            foreach (KeyValuePair<Control, string> item in _texts) item.Key.Text = T(item.Value);
            int selected = _quality.SelectedIndex; _quality.Items.Clear(); _quality.Items.Add(T("quality_low")); _quality.Items.Add(T("quality_classic")); _quality.Items.Add(T("quality_high")); _quality.SelectedIndex = selected < 0 ? 1 : selected;
            Values(); ResizeCards();
        }

        private string T(string key)
        {
            bool en = _language != null && _language.SelectedIndex == 1;
            if (en)
            {
                switch (key) {
                    case "settings": return "Settings"; case "subtitle": return "RETRO SCREENSAVER · SETTINGS"; case "language": return "Language"; case "tab_general": return "General"; case "tab_display": return "Display"; case "tab_engine": return "Engine"; case "palette_section": return "Palette and colour"; case "palette": return "Palette"; case "colors": return "Colours"; case "color_cycle": return "Colour cycling"; case "color_speed": return "Colour speed"; case "motion_section": return "Motion and finish"; case "motion_speed": return "Speed"; case "spatial_scale": return "Spatial scale"; case "warp": return "Warp"; case "pixelation": return "Pixelation"; case "scanlines": return "CRT scanlines"; case "render_section": return "Rendering and performance"; case "quality": return "Quality"; case "target_fps": return "Target FPS"; case "power_saving": return "Power saving · maximum 30 FPS"; case "line_spacing": return "CRT spacing"; case "line_opacity": return "CRT opacity"; case "mirror_h": return "Mirror horizontal"; case "mirror_v": return "Mirror vertical"; case "vignette": return "CRT vignette"; case "engine_section": return "Plasma engine"; case "density": return "Wave density"; case "pulse": return "Radial pulse"; case "rotation": return "Rotation"; case "brightness": return "Brightness"; case "contrast": return "Contrast"; case "origin_section": return "Origin"; case "random_origin": return "Random on start"; case "moving_origin": return "Moving orbit"; case "origin_x": return "Position X"; case "origin_y": return "Position Y"; case "save": return "Save"; case "cancel": return "Cancel"; case "test": return "Test"; case "quality_low": return "Low · GPU at 25%"; case "quality_classic": return "Classic · GPU at 38%"; case "quality_high": return "High · GPU at 50%"; }
            }
            switch (key) {
                case "settings": return "Configuración"; case "subtitle": return "SALVAPANTALLAS RETRO · CONFIGURACIÓN"; case "language": return "Idioma"; case "tab_general": return "General"; case "tab_display": return "Pantalla"; case "tab_engine": return "Motor"; case "palette_section": return "Paleta y color"; case "palette": return "Paleta"; case "colors": return "Colores"; case "color_cycle": return "Evolución cromática"; case "color_speed": return "Ritmo de color"; case "motion_section": return "Movimiento y acabado"; case "motion_speed": return "Velocidad"; case "spatial_scale": return "Escala espacial"; case "warp": return "Deformación"; case "pixelation": return "Pixelado"; case "scanlines": return "Scanlines CRT"; case "render_section": return "Renderizado y rendimiento"; case "quality": return "Calidad"; case "target_fps": return "FPS objetivo"; case "power_saving": return "Ahorro de energía · máximo 30 FPS"; case "line_spacing": return "Separación CRT"; case "line_opacity": return "Opacidad CRT"; case "mirror_h": return "Espejo horizontal"; case "mirror_v": return "Espejo vertical"; case "vignette": return "Viñeta CRT"; case "engine_section": return "Motor de plasma"; case "density": return "Densidad de ondas"; case "pulse": return "Pulso radial"; case "rotation": return "Giro"; case "brightness": return "Brillo"; case "contrast": return "Contraste"; case "origin_section": return "Origen"; case "random_origin": return "Aleatorio al iniciar"; case "moving_origin": return "Órbita móvil"; case "origin_x": return "Posición X"; case "origin_y": return "Posición Y"; case "save": return "Guardar"; case "cancel": return "Cancelar"; case "test": return "Probar"; case "quality_low": return "Baja · GPU al 25%"; case "quality_classic": return "Clásica · GPU al 38%"; case "quality_high": return "Alta · GPU al 50%"; }
            return key;
        }

        private void LoadControls()
        {
            _language.SelectedIndex = String.Equals(_settings.Language, "en", StringComparison.OrdinalIgnoreCase) ? 1 : 0; SelectPalette(_settings.PaletteKey);
            _quality.Items.Clear(); _quality.Items.Add(T("quality_low")); _quality.Items.Add(T("quality_classic")); _quality.Items.Add(T("quality_high"));
            for (int i = 0; i < 4; i++) SetColor(i, _settings.Colors[i]);
            _cycle.Checked = _settings.ColorCycle; _colorSpeed.Value = Clamp((int)Math.Round(_settings.ColorCycleSpeed * 100), 10, 300); _motion.Value = Clamp((int)Math.Round(_settings.MotionSpeed * 100), 15, 250); _scale.Value = Clamp((int)Math.Round(_settings.SpatialScale * 100), 45, 220); _warp.Value = Clamp((int)Math.Round(_settings.Warp * 100), 0, 100); _pixelation.Value = Clamp(_settings.Pixelation, 1, 20); _scanlines.Checked = _settings.Scanlines;
            _density.Value = Clamp((int)Math.Round(_settings.WaveDensity * 100), 50, 200); _pulse.Value = Clamp((int)Math.Round(_settings.RadialPulse * 100), 0, 200); _rotation.Value = Clamp((int)Math.Round(_settings.RotationSpeed * 100), 0, 200); _brightness.Value = Clamp((int)Math.Round(_settings.Brightness * 100), 50, 160); _contrast.Value = Clamp((int)Math.Round(_settings.Contrast * 100), 50, 200);
            _quality.SelectedIndex = Clamp(_settings.RenderQuality - 1, 0, 2); _fps.Value = Clamp(_settings.TargetFps, 20, 75); _saving.Checked = _settings.PowerSaving; _lineSpacing.Value = Clamp(_settings.ScanlineSpacing, 2, 8); _lineOpacity.Value = Clamp(_settings.ScanlineOpacity, 0, 100); _mirrorH.Checked = _settings.MirrorHorizontal; _mirrorV.Checked = _settings.MirrorVertical; _vignette.Checked = _settings.Vignette;
            _random.Checked = _settings.RandomOrigin; _moving.Checked = _settings.MovingOrigin; _originX.Value = Clamp((int)Math.Round(_settings.OriginX * 100), 10, 90); _originY.Value = Clamp((int)Math.Round(_settings.OriginY * 100), 10, 90);
        }

        private void Values()
        {
            _colorSpeedValue.Text = (_colorSpeed.Value / 100.0).ToString("0.00") + "×"; _motionValue.Text = (_motion.Value / 100.0).ToString("0.00") + "×"; _scaleValue.Text = (_scale.Value / 100.0).ToString("0.00") + "×"; _warpValue.Text = _warp.Value + "%"; _pixelationValue.Text = _pixelation.Value + " px"; _densityValue.Text = (_density.Value / 100.0).ToString("0.00") + "×"; _pulseValue.Text = (_pulse.Value / 100.0).ToString("0.00") + "×"; _rotationValue.Text = (_rotation.Value / 100.0).ToString("0.00") + "×"; _brightnessValue.Text = _brightness.Value + "%"; _contrastValue.Text = _contrast.Value + "%"; _fpsValue.Text = _saving.Checked && _fps.Value > 30 ? "30 FPS max." : _fps.Value + " FPS"; _lineSpacingValue.Text = _lineSpacing.Value + " px"; _lineOpacityValue.Text = _lineOpacity.Value + "%"; _originXValue.Text = _originX.Value + "%"; _originYValue.Text = _originY.Value + "%";
        }

        private void OnPalette(object sender, EventArgs e) { if (_loading || _palette.SelectedItem == null) return; PaletteDefinition d = (PaletteDefinition)_palette.SelectedItem; if (d.Key != "custom") { Color[] c = PaletteCatalog.CopyColors(d.Key); for (int i = 0; i < 4; i++) SetColor(i, c[i]); } }
        private void PickColor(object sender, EventArgs e) { Button b = (Button)sender; using (ColorDialog d = new ColorDialog()) { d.Color = b.BackColor; d.FullOpen = true; if (d.ShowDialog(this) == DialogResult.OK) { SetColor((int)b.Tag, d.Color); SelectPalette("custom"); } } }
        private void SetColor(int index, Color color) { _colors[index].BackColor = color; _colors[index].ForeColor = color.GetBrightness() > .58f ? Color.Black : Color.White; }
        private void SelectPalette(string key) { for (int i = 0; i < _palette.Items.Count; i++) if (String.Equals(((PaletteDefinition)_palette.Items[i]).Key, key, StringComparison.OrdinalIgnoreCase)) { _palette.SelectedIndex = i; return; } _palette.SelectedIndex = 0; }

        private void Save(object sender, EventArgs e) { try { Apply(); _settings.Save(); DialogResult = DialogResult.OK; Close(); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Plasma Old School", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
        private void Test(object sender, EventArgs e) { try { Apply(); _settings.Save(); ProcessStartInfo p = new ProcessStartInfo(Application.ExecutablePath, "/s"); p.UseShellExecute = false; Process.Start(p); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Plasma Old School", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
        private void Apply()
        {
            PaletteDefinition p = (PaletteDefinition)_palette.SelectedItem; _settings.PaletteKey = p == null ? "custom" : p.Key; _settings.Language = _language.SelectedIndex == 1 ? "en" : "es"; for (int i = 0; i < 4; i++) _settings.Colors[i] = _colors[i].BackColor;
            _settings.ColorCycle = _cycle.Checked; _settings.ColorCycleSpeed = _colorSpeed.Value / 100.0; _settings.MotionSpeed = _motion.Value / 100.0; _settings.SpatialScale = _scale.Value / 100.0; _settings.Warp = _warp.Value / 100.0; _settings.Pixelation = _pixelation.Value; _settings.Scanlines = _scanlines.Checked; _settings.WaveDensity = _density.Value / 100.0; _settings.RadialPulse = _pulse.Value / 100.0; _settings.RotationSpeed = _rotation.Value / 100.0; _settings.Brightness = _brightness.Value / 100.0; _settings.Contrast = _contrast.Value / 100.0;
            _settings.RenderQuality = _quality.SelectedIndex + 1; _settings.TargetFps = _fps.Value; _settings.PowerSaving = _saving.Checked; _settings.ScanlineSpacing = _lineSpacing.Value; _settings.ScanlineOpacity = _lineOpacity.Value; _settings.MirrorHorizontal = _mirrorH.Checked; _settings.MirrorVertical = _mirrorV.Checked; _settings.Vignette = _vignette.Checked; _settings.RandomOrigin = _random.Checked; _settings.MovingOrigin = _moving.Checked; _settings.OriginX = _originX.Value / 100.0; _settings.OriginY = _originY.Value / 100.0;
        }

        private static int Clamp(int v, int min, int max) { return Math.Max(min, Math.Min(max, v)); }
        private static bool IsDark() { try { return Convert.ToInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1)) == 0; } catch { return false; } }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); try { int v = _dark ? 1 : 0; DwmSetWindowAttribute(Handle, 20, ref v, 4); } catch { } }
        private void Theme()
        {
            Color back = _dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243), card = _dark ? Color.FromArgb(45, 45, 48) : Color.White, text = _dark ? Color.FromArgb(245, 245, 245) : Color.FromArgb(32, 32, 32), accent = _dark ? Color.FromArgb(96, 205, 255) : Color.FromArgb(0, 95, 184);
            BackColor = back; ForeColor = text; PaintTheme(this, back, card, text, accent);
        }
        private void PaintTheme(Control parent, Color back, Color card, Color text, Color accent)
        {
            foreach (Control c in parent.Controls) { Panel panel = c as Panel; if (panel != null && panel.BorderStyle == BorderStyle.FixedSingle) c.BackColor = card; else if (!(c is Button && Array.IndexOf(_colors, c) >= 0)) c.BackColor = back; c.ForeColor = text; Button b = c as Button; if (b != null && Array.IndexOf(_colors, b) < 0) { bool a = Equals(b.Tag, "accent"); b.BackColor = a ? accent : (_dark ? Color.FromArgb(60, 60, 64) : Color.White); b.ForeColor = a ? Color.White : text; b.FlatAppearance.BorderColor = _dark ? Color.FromArgb(90, 90, 95) : Color.FromArgb(200, 200, 200); } PaintTheme(c, back, card, text, accent); }
        }
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr h, int attribute, ref int value, int size);
    }
}

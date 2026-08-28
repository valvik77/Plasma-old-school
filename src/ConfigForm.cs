using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PlasmaOldSchool
{
    internal sealed class ConfigForm : Form
    {
        private readonly PlasmaSettings _settings;
        private readonly ComboBox _paletteCombo;
        private readonly Button[] _colorButtons = new Button[4];
        private readonly CheckBox _colorCycleCheck;
        private readonly TrackBar _colorSpeedTrack;
        private readonly Label _colorSpeedValue;
        private readonly TrackBar _motionSpeedTrack;
        private readonly Label _motionSpeedValue;
        private readonly TrackBar _scaleTrack;
        private readonly Label _scaleValue;
        private readonly TrackBar _warpTrack;
        private readonly Label _warpValue;
        private readonly CheckBox _scanlineCheck;
        private readonly TrackBar _waveDensityTrack;
        private readonly Label _waveDensityValue;
        private readonly TrackBar _radialPulseTrack;
        private readonly Label _radialPulseValue;
        private readonly TrackBar _rotationTrack;
        private readonly Label _rotationValue;
        private readonly TrackBar _brightnessTrack;
        private readonly Label _brightnessValue;
        private readonly TrackBar _contrastTrack;
        private readonly Label _contrastValue;
        private readonly TrackBar _fpsTrack;
        private readonly Label _fpsValue;
        private readonly TrackBar _scanlineSpacingTrack;
        private readonly Label _scanlineSpacingValue;
        private readonly TrackBar _scanlineOpacityTrack;
        private readonly Label _scanlineOpacityValue;
        private readonly ComboBox _qualityCombo;
        private readonly CheckBox _mirrorHorizontalCheck;
        private readonly CheckBox _mirrorVerticalCheck;
        private readonly CheckBox _vignetteCheck;
        private readonly CheckBox _randomOriginCheck;
        private readonly CheckBox _movingOriginCheck;
        private readonly TrackBar _pixelationTrack;
        private readonly Label _pixelationValue;
        private readonly TrackBar _originXTrack;
        private readonly Label _originXValue;
        private readonly TrackBar _originYTrack;
        private readonly Label _originYValue;
        private bool _loading;

        internal ConfigForm()
        {
            _loading = true;
            _settings = PlasmaSettings.Load();

            Text = "Plasma Old School — Configuración";
            ClientSize = new Size(1000, 1000);
            MinimumSize = new Size(1000, 1000);
            MaximumSize = new Size(1000, 1000);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);

            FlowLayoutPanel stack = new FlowLayoutPanel();
            stack.Dock = DockStyle.Fill;
            stack.FlowDirection = FlowDirection.TopDown;
            stack.WrapContents = false;
            stack.AutoScroll = true;
            stack.Padding = new Padding(24);
            Controls.Add(stack);

            Label title = new Label();
            title.Text = "PLASMA OLD SCHOOL";
            title.Font = new Font("Consolas", 17.0f, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = Color.FromArgb(85, 55, 190);
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Size = new Size(920, 42);
            title.Margin = Padding.Empty;
            stack.Controls.Add(title);

            GroupBox paletteGroup = new GroupBox();
            paletteGroup.Text = "Paleta y evolución de color";
            paletteGroup.Size = new Size(920, 190);
            paletteGroup.Margin = Padding.Empty;
            stack.Controls.Add(paletteGroup);

            Label paletteLabel = MakeLabel("Paleta", 20, 35, 140);
            paletteGroup.Controls.Add(paletteLabel);

            _paletteCombo = new ComboBox();
            _paletteCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _paletteCombo.SetBounds(170, 30, 360, 28);
            int presetIndex;
            for (presetIndex = 0; presetIndex < PaletteCatalog.Presets.Length; presetIndex++)
            {
                _paletteCombo.Items.Add(PaletteCatalog.Presets[presetIndex]);
            }
            _paletteCombo.SelectedIndexChanged += OnPaletteChanged;
            paletteGroup.Controls.Add(_paletteCombo);

            paletteGroup.Controls.Add(MakeLabel("Colores", 20, 82, 140));
            FlowLayoutPanel colorRow = new FlowLayoutPanel();
            colorRow.SetBounds(170, 71, 420, 42);
            colorRow.WrapContents = false;
            paletteGroup.Controls.Add(colorRow);

            int colorIndex;
            for (colorIndex = 0; colorIndex < _colorButtons.Length; colorIndex++)
            {
                Button colorButton = new Button();
                colorButton.Width = 62;
                colorButton.Height = 34;
                colorButton.Margin = new Padding(0, 0, 10, 0);
                colorButton.Text = (colorIndex + 1).ToString();
                colorButton.Tag = colorIndex;
                colorButton.FlatStyle = FlatStyle.Flat;
                colorButton.Click += OnChooseColor;
                _colorButtons[colorIndex] = colorButton;
                colorRow.Controls.Add(colorButton);
            }

            _colorCycleCheck = new CheckBox();
            _colorCycleCheck.Text = "Evolución cromática";
            _colorCycleCheck.AutoSize = true;
            _colorCycleCheck.SetBounds(20, 137, 190, 26);
            _colorCycleCheck.CheckedChanged += OnColorCycleChanged;
            paletteGroup.Controls.Add(_colorCycleCheck);

            paletteGroup.Controls.Add(MakeLabel("Ritmo", 235, 139, 60));
            _colorSpeedTrack = MakeTrackBar(10, 300, 5, 295, 125, 220);
            _colorSpeedTrack.ValueChanged += delegate { UpdateValueLabels(); };
            paletteGroup.Controls.Add(_colorSpeedTrack);
            _colorSpeedValue = MakeLabel(String.Empty, 525, 139, 75);
            paletteGroup.Controls.Add(_colorSpeedValue);

            GroupBox motionGroup = new GroupBox();
            motionGroup.Text = "Movimiento y acabado CRT";
            motionGroup.Size = new Size(920, 280);
            motionGroup.Margin = Padding.Empty;
            stack.Controls.Add(motionGroup);

            _motionSpeedTrack = MakeTrackBar(15, 250, 5, 170, 24, 580);
            _motionSpeedValue = MakeLabel(String.Empty, 780, 39, 85);
            AddSliderRow(motionGroup, "Velocidad", 24, _motionSpeedTrack, _motionSpeedValue);

            _scaleTrack = MakeTrackBar(45, 220, 5, 170, 78, 580);
            _scaleValue = MakeLabel(String.Empty, 780, 93, 85);
            AddSliderRow(motionGroup, "Escala espacial", 78, _scaleTrack, _scaleValue);

            _warpTrack = MakeTrackBar(0, 100, 5, 170, 132, 580);
            _warpValue = MakeLabel(String.Empty, 780, 147, 85);
            AddSliderRow(motionGroup, "Deformación", 132, _warpTrack, _warpValue);

            _motionSpeedTrack.ValueChanged += delegate { UpdateValueLabels(); };
            _scaleTrack.ValueChanged += delegate { UpdateValueLabels(); };
            _warpTrack.ValueChanged += delegate { UpdateValueLabels(); };

            _scanlineCheck = new CheckBox();
            _scanlineCheck.Text = "Scanlines CRT";
            _scanlineCheck.AutoSize = true;
            _scanlineCheck.SetBounds(20, 190, 180, 26);
            motionGroup.Controls.Add(_scanlineCheck);

            motionGroup.Controls.Add(MakeLabel("Pixelado", 300, 220, 120));
            _pixelationTrack = MakeTrackBar(1, 20, 1, 440, 205, 250);
            _pixelationValue = MakeLabel(String.Empty, 720, 220, 70);
            _pixelationTrack.ValueChanged += delegate { UpdateValueLabels(); };
            motionGroup.Controls.Add(_pixelationTrack);
            motionGroup.Controls.Add(_pixelationValue);

            GroupBox advancedGroup = new GroupBox();
            advancedGroup.Text = "Motor avanzado y salida";
            advancedGroup.Size = new Size(920, 360);
            advancedGroup.Margin = Padding.Empty;
            stack.Controls.Add(advancedGroup);

            _waveDensityTrack = MakeTrackBar(50, 200, 5, 120, 17, 180);
            _waveDensityValue = MakeLabel(String.Empty, 310, 32, 55);
            AddCompactSliderRow(advancedGroup, "Densidad", 17, _waveDensityTrack, _waveDensityValue, 20, 120, 310, 180);

            _radialPulseTrack = MakeTrackBar(0, 200, 5, 120, 77, 180);
            _radialPulseValue = MakeLabel(String.Empty, 310, 92, 55);
            AddCompactSliderRow(advancedGroup, "Pulso radial", 77, _radialPulseTrack, _radialPulseValue, 20, 120, 310, 180);

            _rotationTrack = MakeTrackBar(0, 200, 5, 120, 137, 180);
            _rotationValue = MakeLabel(String.Empty, 310, 152, 55);
            AddCompactSliderRow(advancedGroup, "Giro", 137, _rotationTrack, _rotationValue, 20, 120, 310, 180);

            _brightnessTrack = MakeTrackBar(50, 160, 5, 120, 197, 180);
            _brightnessValue = MakeLabel(String.Empty, 310, 212, 55);
            AddCompactSliderRow(advancedGroup, "Brillo", 197, _brightnessTrack, _brightnessValue, 20, 120, 310, 180);

            _contrastTrack = MakeTrackBar(50, 200, 5, 500, 17, 180);
            _contrastValue = MakeLabel(String.Empty, 690, 32, 55);
            AddCompactSliderRow(advancedGroup, "Contraste", 17, _contrastTrack, _contrastValue, 380, 500, 690, 180);

            _fpsTrack = MakeTrackBar(20, 75, 5, 500, 77, 180);
            _fpsValue = MakeLabel(String.Empty, 690, 92, 55);
            AddCompactSliderRow(advancedGroup, "FPS", 77, _fpsTrack, _fpsValue, 380, 500, 690, 180);

            _scanlineSpacingTrack = MakeTrackBar(2, 8, 1, 500, 137, 180);
            _scanlineSpacingValue = MakeLabel(String.Empty, 690, 152, 55);
            AddCompactSliderRow(advancedGroup, "Línea CRT", 137, _scanlineSpacingTrack, _scanlineSpacingValue, 380, 500, 690, 180);

            _scanlineOpacityTrack = MakeTrackBar(0, 100, 5, 500, 197, 180);
            _scanlineOpacityValue = MakeLabel(String.Empty, 690, 212, 55);
            AddCompactSliderRow(advancedGroup, "Opacidad CRT", 197, _scanlineOpacityTrack, _scanlineOpacityValue, 380, 500, 690, 180);

            advancedGroup.Controls.Add(MakeLabel("Calidad", 20, 266, 80));
            _qualityCombo = new ComboBox();
            _qualityCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _qualityCombo.Items.Add("Baja · 160 × 90");
            _qualityCombo.Items.Add("Clásica · 256 × 144");
            _qualityCombo.Items.Add("Alta · 320 × 180");
            _qualityCombo.SetBounds(110, 261, 190, 28);
            advancedGroup.Controls.Add(_qualityCombo);

            _mirrorHorizontalCheck = new CheckBox();
            _mirrorHorizontalCheck.Text = "Espejo H";
            _mirrorHorizontalCheck.AutoSize = true;
            _mirrorHorizontalCheck.SetBounds(330, 266, 90, 26);
            advancedGroup.Controls.Add(_mirrorHorizontalCheck);

            _mirrorVerticalCheck = new CheckBox();
            _mirrorVerticalCheck.Text = "Espejo V";
            _mirrorVerticalCheck.AutoSize = true;
            _mirrorVerticalCheck.SetBounds(440, 266, 90, 26);
            advancedGroup.Controls.Add(_mirrorVerticalCheck);

            _vignetteCheck = new CheckBox();
            _vignetteCheck.Text = "Viñeta CRT";
            _vignetteCheck.AutoSize = true;
            _vignetteCheck.SetBounds(550, 266, 110, 26);
            advancedGroup.Controls.Add(_vignetteCheck);

            _randomOriginCheck = new CheckBox();
            _randomOriginCheck.Text = "Origen aleatorio al iniciar";
            _randomOriginCheck.AutoSize = true;
            _randomOriginCheck.SetBounds(20, 300, 205, 26);
            _randomOriginCheck.CheckedChanged += OnRandomOriginChanged;
            advancedGroup.Controls.Add(_randomOriginCheck);

            _movingOriginCheck = new CheckBox();
            _movingOriginCheck.Text = "Órbita móvil";
            _movingOriginCheck.AutoSize = true;
            _movingOriginCheck.SetBounds(250, 300, 170, 26);
            advancedGroup.Controls.Add(_movingOriginCheck);

            advancedGroup.Controls.Add(MakeLabel("Origen X", 450, 300, 70));
            _originXTrack = MakeTrackBar(10, 90, 5, 520, 283, 110);
            _originXValue = MakeLabel(String.Empty, 645, 300, 55);
            advancedGroup.Controls.Add(_originXTrack);
            advancedGroup.Controls.Add(_originXValue);

            advancedGroup.Controls.Add(MakeLabel("Origen Y", 700, 300, 70));
            _originYTrack = MakeTrackBar(10, 90, 5, 780, 283, 90);
            _originYValue = MakeLabel(String.Empty, 880, 300, 45);
            advancedGroup.Controls.Add(_originYTrack);
            advancedGroup.Controls.Add(_originYValue);

            _waveDensityTrack.ValueChanged += delegate { UpdateValueLabels(); };
            _radialPulseTrack.ValueChanged += delegate { UpdateValueLabels(); };
            _rotationTrack.ValueChanged += delegate { UpdateValueLabels(); };
            _brightnessTrack.ValueChanged += delegate { UpdateValueLabels(); };
            _contrastTrack.ValueChanged += delegate { UpdateValueLabels(); };
            _fpsTrack.ValueChanged += delegate { UpdateValueLabels(); };
            _scanlineSpacingTrack.ValueChanged += delegate { UpdateValueLabels(); };
            _scanlineOpacityTrack.ValueChanged += delegate { UpdateValueLabels(); };
            _originXTrack.ValueChanged += delegate { UpdateValueLabels(); };
            _originYTrack.ValueChanged += delegate { UpdateValueLabels(); };

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Size = new Size(920, 55);
            buttons.Margin = Padding.Empty;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            stack.Controls.Add(buttons);

            Button saveButton = MakeActionButton("Guardar", 105);
            saveButton.Click += OnSave;
            buttons.Controls.Add(saveButton);
            AcceptButton = saveButton;

            Button cancelButton = MakeActionButton("Cancelar", 105);
            cancelButton.Click += OnCancel;
            buttons.Controls.Add(cancelButton);
            CancelButton = cancelButton;

            Button testButton = MakeActionButton("Probar", 105);
            testButton.Click += OnTest;
            buttons.Controls.Add(testButton);

            LoadSettingsIntoControls();
            _loading = false;
            UpdateValueLabels();
            OnColorCycleChanged(this, EventArgs.Empty);
        }

        private void LoadSettingsIntoControls()
        {
            SelectPalette(_settings.PaletteKey);
            int i;
            for (i = 0; i < _colorButtons.Length; i++)
            {
                SetColorButton(i, _settings.Colors[i]);
            }
            _colorCycleCheck.Checked = _settings.ColorCycle;
            _colorSpeedTrack.Value = Clamp((int)Math.Round(_settings.ColorCycleSpeed * 100.0), _colorSpeedTrack.Minimum, _colorSpeedTrack.Maximum);
            _motionSpeedTrack.Value = Clamp((int)Math.Round(_settings.MotionSpeed * 100.0), _motionSpeedTrack.Minimum, _motionSpeedTrack.Maximum);
            _scaleTrack.Value = Clamp((int)Math.Round(_settings.SpatialScale * 100.0), _scaleTrack.Minimum, _scaleTrack.Maximum);
            _warpTrack.Value = Clamp((int)Math.Round(_settings.Warp * 100.0), _warpTrack.Minimum, _warpTrack.Maximum);
            _scanlineCheck.Checked = _settings.Scanlines;
            _waveDensityTrack.Value = Clamp((int)Math.Round(_settings.WaveDensity * 100.0), _waveDensityTrack.Minimum, _waveDensityTrack.Maximum);
            _radialPulseTrack.Value = Clamp((int)Math.Round(_settings.RadialPulse * 100.0), _radialPulseTrack.Minimum, _radialPulseTrack.Maximum);
            _rotationTrack.Value = Clamp((int)Math.Round(_settings.RotationSpeed * 100.0), _rotationTrack.Minimum, _rotationTrack.Maximum);
            _brightnessTrack.Value = Clamp((int)Math.Round(_settings.Brightness * 100.0), _brightnessTrack.Minimum, _brightnessTrack.Maximum);
            _contrastTrack.Value = Clamp((int)Math.Round(_settings.Contrast * 100.0), _contrastTrack.Minimum, _contrastTrack.Maximum);
            _fpsTrack.Value = Clamp(_settings.TargetFps, _fpsTrack.Minimum, _fpsTrack.Maximum);
            _scanlineSpacingTrack.Value = Clamp(_settings.ScanlineSpacing, _scanlineSpacingTrack.Minimum, _scanlineSpacingTrack.Maximum);
            _scanlineOpacityTrack.Value = Clamp(_settings.ScanlineOpacity, _scanlineOpacityTrack.Minimum, _scanlineOpacityTrack.Maximum);
            _qualityCombo.SelectedIndex = Clamp(_settings.RenderQuality - 1, 0, 2);
            _mirrorHorizontalCheck.Checked = _settings.MirrorHorizontal;
            _mirrorVerticalCheck.Checked = _settings.MirrorVertical;
            _vignetteCheck.Checked = _settings.Vignette;
            _randomOriginCheck.Checked = _settings.RandomOrigin;
            _movingOriginCheck.Checked = _settings.MovingOrigin;
            _pixelationTrack.Value = Clamp(_settings.Pixelation, _pixelationTrack.Minimum, _pixelationTrack.Maximum);
            _originXTrack.Value = Clamp((int)Math.Round(_settings.OriginX * 100.0), _originXTrack.Minimum, _originXTrack.Maximum);
            _originYTrack.Value = Clamp((int)Math.Round(_settings.OriginY * 100.0), _originYTrack.Minimum, _originYTrack.Maximum);
        }

        private void OnPaletteChanged(object sender, EventArgs e)
        {
            if (_loading || _paletteCombo.SelectedItem == null)
            {
                return;
            }

            PaletteDefinition selected = (PaletteDefinition)_paletteCombo.SelectedItem;
            if (!String.Equals(selected.Key, "custom", StringComparison.OrdinalIgnoreCase))
            {
                Color[] colors = PaletteCatalog.CopyColors(selected.Key);
                int i;
                for (i = 0; i < _colorButtons.Length; i++)
                {
                    SetColorButton(i, colors[i]);
                }
            }
        }

        private void OnChooseColor(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.Color = button.BackColor;
                dialog.FullOpen = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    SetColorButton((int)button.Tag, dialog.Color);
                    SelectPalette("custom");
                }
            }
        }

        private void OnColorCycleChanged(object sender, EventArgs e)
        {
            _colorSpeedTrack.Enabled = _colorCycleCheck.Checked;
        }

        private void OnSave(object sender, EventArgs e)
        {
            try
            {
                ApplyControlsToSettings();
                _settings.Save();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Plasma Old School", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnTest(object sender, EventArgs e)
        {
            try
            {
                ApplyControlsToSettings();
                _settings.Save();
                ProcessStartInfo startInfo = new ProcessStartInfo(Application.ExecutablePath, "/s");
                startInfo.UseShellExecute = false;
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Plasma Old School", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCancel(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ApplyControlsToSettings()
        {
            PaletteDefinition selected = (PaletteDefinition)_paletteCombo.SelectedItem;
            _settings.PaletteKey = selected == null ? "custom" : selected.Key;
            int i;
            for (i = 0; i < _colorButtons.Length; i++)
            {
                _settings.Colors[i] = _colorButtons[i].BackColor;
            }
            _settings.ColorCycle = _colorCycleCheck.Checked;
            _settings.ColorCycleSpeed = _colorSpeedTrack.Value / 100.0;
            _settings.MotionSpeed = _motionSpeedTrack.Value / 100.0;
            _settings.SpatialScale = _scaleTrack.Value / 100.0;
            _settings.Warp = _warpTrack.Value / 100.0;
            _settings.Scanlines = _scanlineCheck.Checked;
            _settings.WaveDensity = _waveDensityTrack.Value / 100.0;
            _settings.RadialPulse = _radialPulseTrack.Value / 100.0;
            _settings.RotationSpeed = _rotationTrack.Value / 100.0;
            _settings.Brightness = _brightnessTrack.Value / 100.0;
            _settings.Contrast = _contrastTrack.Value / 100.0;
            _settings.TargetFps = _fpsTrack.Value;
            _settings.ScanlineSpacing = _scanlineSpacingTrack.Value;
            _settings.ScanlineOpacity = _scanlineOpacityTrack.Value;
            _settings.RenderQuality = _qualityCombo.SelectedIndex + 1;
            _settings.MirrorHorizontal = _mirrorHorizontalCheck.Checked;
            _settings.MirrorVertical = _mirrorVerticalCheck.Checked;
            _settings.Vignette = _vignetteCheck.Checked;
            _settings.RandomOrigin = _randomOriginCheck.Checked;
            _settings.MovingOrigin = _movingOriginCheck.Checked;
            _settings.Pixelation = _pixelationTrack.Value;
            _settings.OriginX = _originXTrack.Value / 100.0;
            _settings.OriginY = _originYTrack.Value / 100.0;
        }

        private void UpdateValueLabels()
        {
            _colorSpeedValue.Text = (_colorSpeedTrack.Value / 100.0).ToString("0.00") + "×";
            _motionSpeedValue.Text = (_motionSpeedTrack.Value / 100.0).ToString("0.00") + "×";
            _scaleValue.Text = (_scaleTrack.Value / 100.0).ToString("0.00") + "×";
            _warpValue.Text = _warpTrack.Value.ToString() + "%";
            _waveDensityValue.Text = (_waveDensityTrack.Value / 100.0).ToString("0.00") + "×";
            _radialPulseValue.Text = (_radialPulseTrack.Value / 100.0).ToString("0.00") + "×";
            _rotationValue.Text = (_rotationTrack.Value / 100.0).ToString("0.00") + "×";
            _brightnessValue.Text = _brightnessTrack.Value.ToString() + "%";
            _contrastValue.Text = _contrastTrack.Value.ToString() + "%";
            _fpsValue.Text = _fpsTrack.Value.ToString() + " FPS";
            _scanlineSpacingValue.Text = _scanlineSpacingTrack.Value.ToString() + " px";
            _scanlineOpacityValue.Text = _scanlineOpacityTrack.Value.ToString() + "%";
            _originXValue.Text = _originXTrack.Value.ToString() + "%";
            _originYValue.Text = _originYTrack.Value.ToString() + "%";
            _pixelationValue.Text = _pixelationTrack.Value.ToString() + " px";
        }

        private void SelectPalette(string key)
        {
            int i;
            for (i = 0; i < _paletteCombo.Items.Count; i++)
            {
                PaletteDefinition definition = (PaletteDefinition)_paletteCombo.Items[i];
                if (String.Equals(definition.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    _paletteCombo.SelectedIndex = i;
                    return;
                }
            }
            _paletteCombo.SelectedIndex = 0;
        }

        private void SetColorButton(int index, Color color)
        {
            _colorButtons[index].BackColor = color;
            _colorButtons[index].ForeColor = color.GetBrightness() > 0.58f ? Color.Black : Color.White;
        }

        private void OnRandomOriginChanged(object sender, EventArgs e)
        {
            _originXTrack.Enabled = !_randomOriginCheck.Checked;
            _originYTrack.Enabled = !_randomOriginCheck.Checked;
        }

        private static Label MakeLabel(string text, int x, int y, int width)
        {
            Label label = new Label();
            label.Text = text;
            label.SetBounds(x, y, width, 24);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private static TrackBar MakeTrackBar(int minimum, int maximum, int smallChange, int x, int y, int width)
        {
            TrackBar track = new TrackBar();
            // Evita que el tamaño automático de WinForms recorte el pulgar
            // cuando el control se coloca dentro de un GroupBox compacto.
            track.AutoSize = false;
            track.Minimum = minimum;
            track.Maximum = maximum;
            track.SmallChange = smallChange;
            track.LargeChange = smallChange * 4;
            track.TickFrequency = Math.Max(1, (maximum - minimum) / 8);
            track.SetBounds(x, y, width, 45);
            return track;
        }

        private static void AddSliderRow(Control parent, string labelText, int y, TrackBar track, Label valueLabel)
        {
            parent.Controls.Add(MakeLabel(labelText, 20, y + 15, 140));
            parent.Controls.Add(track);
            parent.Controls.Add(valueLabel);
        }

        private static void AddCompactSliderRow(Control parent, string labelText, int y, TrackBar track, Label valueLabel, int labelX, int trackX, int valueX, int trackWidth)
        {
            parent.Controls.Add(MakeLabel(labelText, labelX, y + 15, 95));
            track.SetBounds(trackX, y, trackWidth, 45);
            valueLabel.SetBounds(valueX, y + 15, 70, 24);
            parent.Controls.Add(track);
            parent.Controls.Add(valueLabel);
        }

        private static Button MakeActionButton(string text, int width)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = width;
            button.Height = 34;
            button.Margin = new Padding(8, 8, 0, 0);
            return button;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}

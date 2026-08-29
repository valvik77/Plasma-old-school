using System;
using System.Collections.Generic;
using System.ComponentModel;
using WinColor = Windows.UI.Color;

namespace PlasmaOldSchool.Config
{
    public sealed class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public global::PlasmaOldSchool.PlasmaSettings Settings { get; }
        public IEnumerable<global::PlasmaOldSchool.PaletteDefinition> Palettes => global::PlasmaOldSchool.PaletteCatalog.Presets;

        public SettingsViewModel(global::PlasmaOldSchool.PlasmaSettings settings) { Settings = settings; }
        private void Changed(string name) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        private void Set<T>(ref T field, T value, string name) { if (!EqualityComparer<T>.Default.Equals(field, value)) { field = value; Changed(name); } }

        public string PaletteKey { get => Settings.PaletteKey; set { if (Settings.PaletteKey != value) { Settings.PaletteKey = value; if (!String.Equals(value, "custom", StringComparison.OrdinalIgnoreCase)) Settings.Colors = global::PlasmaOldSchool.PaletteCatalog.CopyColors(value); Changed(nameof(PaletteKey)); Changed(nameof(Color1)); Changed(nameof(Color2)); Changed(nameof(Color3)); Changed(nameof(Color4)); } } }
        public string Language { get => Settings.Language; set => Set(ref Settings.Language, value, nameof(Language)); }
        public double MotionSpeed { get => Settings.MotionSpeed; set => Set(ref Settings.MotionSpeed, value, nameof(MotionSpeed)); }
        public double SpatialScale { get => Settings.SpatialScale; set => Set(ref Settings.SpatialScale, value, nameof(SpatialScale)); }
        public double Warp { get => Settings.Warp; set => Set(ref Settings.Warp, value, nameof(Warp)); }
        public bool ColorCycle { get => Settings.ColorCycle; set => Set(ref Settings.ColorCycle, value, nameof(ColorCycle)); }
        public bool RgbPaletteCycle { get => Settings.RgbPaletteCycle; set => Set(ref Settings.RgbPaletteCycle, value, nameof(RgbPaletteCycle)); }
        public double ColorCycleSpeed { get => Settings.ColorCycleSpeed; set => Set(ref Settings.ColorCycleSpeed, value, nameof(ColorCycleSpeed)); }
        public int Pixelation { get => Settings.Pixelation; set => Set(ref Settings.Pixelation, value, nameof(Pixelation)); }
        public int RenderQuality { get => Settings.RenderQuality; set => Set(ref Settings.RenderQuality, value, nameof(RenderQuality)); }
        public int RenderQualityIndex
        {
            get { return Settings.RenderQuality - 1; }
            set
            {
                int quality = value + 1;
                if (Settings.RenderQuality != quality)
                {
                    Settings.RenderQuality = quality;
                    Changed(nameof(RenderQualityIndex));
                    Changed(nameof(RenderQuality));
                }
            }
        }
        public int TargetFps { get => Settings.TargetFps; set => Set(ref Settings.TargetFps, value, nameof(TargetFps)); }
        public bool PowerSaving { get => Settings.PowerSaving; set => Set(ref Settings.PowerSaving, value, nameof(PowerSaving)); }
        public bool Scanlines { get => Settings.Scanlines; set => Set(ref Settings.Scanlines, value, nameof(Scanlines)); }
        public int ScanlineSpacing { get => Settings.ScanlineSpacing; set => Set(ref Settings.ScanlineSpacing, value, nameof(ScanlineSpacing)); }
        public int ScanlineOpacity { get => Settings.ScanlineOpacity; set => Set(ref Settings.ScanlineOpacity, value, nameof(ScanlineOpacity)); }
        public bool Vignette { get => Settings.Vignette; set => Set(ref Settings.Vignette, value, nameof(Vignette)); }
        public bool MirrorHorizontal { get => Settings.MirrorHorizontal; set => Set(ref Settings.MirrorHorizontal, value, nameof(MirrorHorizontal)); }
        public bool MirrorVertical { get => Settings.MirrorVertical; set => Set(ref Settings.MirrorVertical, value, nameof(MirrorVertical)); }
        public double WaveDensity { get => Settings.WaveDensity; set => Set(ref Settings.WaveDensity, value, nameof(WaveDensity)); }
        public double RadialPulse { get => Settings.RadialPulse; set => Set(ref Settings.RadialPulse, value, nameof(RadialPulse)); }
        public double RotationSpeed { get => Settings.RotationSpeed; set => Set(ref Settings.RotationSpeed, value, nameof(RotationSpeed)); }
        public double Brightness { get => Settings.Brightness; set => Set(ref Settings.Brightness, value, nameof(Brightness)); }
        public double Contrast { get => Settings.Contrast; set => Set(ref Settings.Contrast, value, nameof(Contrast)); }
        public bool RandomOrigin { get => Settings.RandomOrigin; set => Set(ref Settings.RandomOrigin, value, nameof(RandomOrigin)); }
        public bool MovingOrigin { get => Settings.MovingOrigin; set => Set(ref Settings.MovingOrigin, value, nameof(MovingOrigin)); }
        public double OriginX { get => Settings.OriginX; set => Set(ref Settings.OriginX, value, nameof(OriginX)); }
        public double OriginY { get => Settings.OriginY; set => Set(ref Settings.OriginY, value, nameof(OriginY)); }

        public WinColor Color1 { get => ToWin(0); set => SetColor(0, value, nameof(Color1)); }
        public WinColor Color2 { get => ToWin(1); set => SetColor(1, value, nameof(Color2)); }
        public WinColor Color3 { get => ToWin(2); set => SetColor(2, value, nameof(Color3)); }
        public WinColor Color4 { get => ToWin(3); set => SetColor(3, value, nameof(Color4)); }
        private WinColor ToWin(int i) { System.Drawing.Color c = Settings.Colors[i]; return WinColor.FromArgb(c.A, c.R, c.G, c.B); }
        private void SetColor(int i, WinColor c, string name) { Settings.Colors[i] = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B); Settings.PaletteKey = "custom"; Changed(name); Changed(nameof(PaletteKey)); }
        public void Save() { Settings.Validate(); Settings.Save(); }
    }
}

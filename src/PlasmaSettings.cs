using System;
using System.Drawing;
using System.Globalization;
using Microsoft.Win32;

namespace PlasmaOldSchool
{
    internal sealed class PaletteDefinition
    {
        public readonly string Key;
        public readonly string DisplayName;
        public readonly Color[] Colors;

        public PaletteDefinition(string key, string displayName, params Color[] colors)
        {
            Key = key;
            DisplayName = displayName;
            Colors = colors;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    internal static class PaletteCatalog
    {
        internal static readonly PaletteDefinition[] Presets =
        {
            new PaletteDefinition("amiga", "Amiga sunset",
                Color.FromArgb(18, 0, 47), Color.FromArgb(255, 20, 111),
                Color.FromArgb(255, 204, 51), Color.FromArgb(0, 229, 255)),
            new PaletteDefinition("ice", "Blue ice",
                Color.FromArgb(3, 5, 29), Color.FromArgb(18, 72, 199),
                Color.FromArgb(83, 215, 255), Color.FromArgb(230, 251, 255)),
            new PaletteDefinition("copper", "Copper bars",
                Color.FromArgb(22, 4, 7), Color.FromArgb(124, 29, 24),
                Color.FromArgb(239, 113, 54), Color.FromArgb(255, 210, 122)),
            new PaletteDefinition("acid", "Acid rave",
                Color.FromArgb(18, 0, 31), Color.FromArgb(77, 255, 0),
                Color.FromArgb(255, 245, 0), Color.FromArgb(255, 0, 170)),
            new PaletteDefinition("rgb", "RGB spectrum",
                Color.FromArgb(255, 0, 0), Color.FromArgb(0, 255, 0),
                Color.FromArgb(0, 0, 255), Color.FromArgb(255, 0, 255)),
            new PaletteDefinition("fire", "Fuego",
                Color.FromArgb(20, 0, 0), Color.FromArgb(150, 8, 0),
                Color.FromArgb(255, 74, 0), Color.FromArgb(255, 238, 96)),
            new PaletteDefinition("forest", "Bosque eléctrico",
                Color.FromArgb(0, 10, 4), Color.FromArgb(0, 88, 35),
                Color.FromArgb(40, 220, 84), Color.FromArgb(202, 255, 136)),
            new PaletteDefinition("ocean", "Océano profundo",
                Color.FromArgb(0, 4, 25), Color.FromArgb(0, 65, 128),
                Color.FromArgb(0, 205, 210), Color.FromArgb(164, 255, 244)),
            new PaletteDefinition("violet", "Violeta neón",
                Color.FromArgb(17, 0, 42), Color.FromArgb(91, 0, 181),
                Color.FromArgb(234, 30, 255), Color.FromArgb(255, 174, 238)),
            new PaletteDefinition("terminal", "Terminal fósforo",
                Color.FromArgb(0, 4, 0), Color.FromArgb(0, 45, 12),
                Color.FromArgb(0, 190, 46), Color.FromArgb(174, 255, 179)),
            new PaletteDefinition("mono", "Monocromo",
                Color.FromArgb(0, 0, 0), Color.FromArgb(45, 45, 45),
                Color.FromArgb(160, 160, 160), Color.FromArgb(255, 255, 255)),
            new PaletteDefinition("custom", "Personalizada")
        };

        internal static PaletteDefinition Find(string key)
        {
            int i;
            for (i = 0; i < Presets.Length; i++)
            {
                if (String.Equals(Presets[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return Presets[i];
                }
            }
            return Presets[0];
        }

        internal static Color[] CopyColors(string key)
        {
            PaletteDefinition definition = Find(key);
            if (definition.Colors == null || definition.Colors.Length != 4)
            {
                definition = Presets[0];
            }
            return (Color[])definition.Colors.Clone();
        }
    }

    internal sealed class PlasmaSettings
    {
        private const string RegistryPath = @"Software\PlasmaOldSchoolScreenSaver";

        public string PaletteKey = "amiga";
        public Color[] Colors = PaletteCatalog.CopyColors("amiga");
        public double MotionSpeed = 1.0;
        public double SpatialScale = 1.0;
        public double Warp = 0.65;
        public bool Scanlines = true;
        public bool ColorCycle = true;
        public double ColorCycleSpeed = 1.0;
        public double WaveDensity = 1.0;
        public double RadialPulse = 1.0;
        public double RotationSpeed = 0.44;
        public double Brightness = 1.0;
        public double Contrast = 1.0;
        public int RenderQuality = 2;
        public int TargetFps = 50;
        public int ScanlineSpacing = 4;
        public int ScanlineOpacity = 42;
        public bool MirrorHorizontal = false;
        public bool MirrorVertical = false;
        public bool Vignette = true;
        public bool RandomOrigin = true;
        public double OriginX = 0.5;
        public double OriginY = 0.5;
        public bool MovingOrigin = true;
        public int Pixelation = 1;

        internal static PlasmaSettings Load()
        {
            PlasmaSettings settings = new PlasmaSettings();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key == null)
                    {
                        return settings;
                    }

                    settings.PaletteKey = Convert.ToString(key.GetValue("Palette", settings.PaletteKey), CultureInfo.InvariantCulture);
                    settings.MotionSpeed = ReadDouble(key, "MotionSpeed", settings.MotionSpeed);
                    settings.SpatialScale = ReadDouble(key, "SpatialScale", settings.SpatialScale);
                    settings.Warp = ReadDouble(key, "Warp", settings.Warp);
                    settings.ColorCycleSpeed = ReadDouble(key, "ColorCycleSpeed", settings.ColorCycleSpeed);
                    settings.WaveDensity = ReadDouble(key, "WaveDensity", settings.WaveDensity);
                    settings.RadialPulse = ReadDouble(key, "RadialPulse", settings.RadialPulse);
                    settings.RotationSpeed = ReadDouble(key, "RotationSpeed", settings.RotationSpeed);
                    settings.Brightness = ReadDouble(key, "Brightness", settings.Brightness);
                    settings.Contrast = ReadDouble(key, "Contrast", settings.Contrast);
                    settings.RenderQuality = ReadInt(key, "RenderQuality", settings.RenderQuality);
                    settings.TargetFps = ReadInt(key, "TargetFps", settings.TargetFps);
                    settings.ScanlineSpacing = ReadInt(key, "ScanlineSpacing", settings.ScanlineSpacing);
                    settings.ScanlineOpacity = ReadInt(key, "ScanlineOpacity", settings.ScanlineOpacity);
                    settings.Scanlines = ReadBool(key, "Scanlines", settings.Scanlines);
                    settings.ColorCycle = ReadBool(key, "ColorCycle", settings.ColorCycle);
                    settings.MirrorHorizontal = ReadBool(key, "MirrorHorizontal", settings.MirrorHorizontal);
                    settings.MirrorVertical = ReadBool(key, "MirrorVertical", settings.MirrorVertical);
                    settings.Vignette = ReadBool(key, "Vignette", settings.Vignette);
                    settings.RandomOrigin = ReadBool(key, "RandomOrigin", settings.RandomOrigin);
                    settings.OriginX = ReadDouble(key, "OriginX", settings.OriginX);
                    settings.OriginY = ReadDouble(key, "OriginY", settings.OriginY);
                    settings.MovingOrigin = ReadBool(key, "MovingOrigin", settings.MovingOrigin);
                    settings.Pixelation = ReadInt(key, "Pixelation", settings.Pixelation);

                    int i;
                    for (i = 0; i < 4; i++)
                    {
                        settings.Colors[i] = ReadColor(key, "Color" + (i + 1).ToString(CultureInfo.InvariantCulture), settings.Colors[i]);
                    }
                }
            }
            catch
            {
                // Si el registro no está disponible se usan valores seguros.
            }

            settings.MotionSpeed = Clamp(settings.MotionSpeed, 0.15, 2.5);
            settings.SpatialScale = Clamp(settings.SpatialScale, 0.45, 2.2);
            settings.Warp = Clamp(settings.Warp, 0.0, 1.0);
            settings.ColorCycleSpeed = Clamp(settings.ColorCycleSpeed, 0.1, 3.0);
            settings.WaveDensity = Clamp(settings.WaveDensity, 0.5, 2.0);
            settings.RadialPulse = Clamp(settings.RadialPulse, 0.0, 2.0);
            settings.RotationSpeed = Clamp(settings.RotationSpeed, 0.0, 2.0);
            settings.Brightness = Clamp(settings.Brightness, 0.5, 1.6);
            settings.Contrast = Clamp(settings.Contrast, 0.5, 2.0);
            settings.RenderQuality = Clamp(settings.RenderQuality, 1, 3);
            settings.TargetFps = Clamp(settings.TargetFps, 20, 75);
            settings.ScanlineSpacing = Clamp(settings.ScanlineSpacing, 2, 8);
            settings.ScanlineOpacity = Clamp(settings.ScanlineOpacity, 0, 100);
            settings.OriginX = Clamp(settings.OriginX, 0.1, 0.9);
            settings.OriginY = Clamp(settings.OriginY, 0.1, 0.9);
            settings.Pixelation = Clamp(settings.Pixelation, 1, 20);
            return settings;
        }

        internal void Save()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
            {
                if (key == null)
                {
                    throw new InvalidOperationException("Windows no permitió guardar la configuración.");
                }

                key.SetValue("Palette", PaletteKey, RegistryValueKind.String);
                key.SetValue("MotionSpeed", MotionSpeed.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                key.SetValue("SpatialScale", SpatialScale.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                key.SetValue("Warp", Warp.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                key.SetValue("ColorCycleSpeed", ColorCycleSpeed.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                key.SetValue("WaveDensity", WaveDensity.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                key.SetValue("RadialPulse", RadialPulse.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                key.SetValue("RotationSpeed", RotationSpeed.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                key.SetValue("Brightness", Brightness.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                key.SetValue("Contrast", Contrast.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                key.SetValue("RenderQuality", RenderQuality, RegistryValueKind.DWord);
                key.SetValue("TargetFps", TargetFps, RegistryValueKind.DWord);
                key.SetValue("ScanlineSpacing", ScanlineSpacing, RegistryValueKind.DWord);
                key.SetValue("ScanlineOpacity", ScanlineOpacity, RegistryValueKind.DWord);
                key.SetValue("Scanlines", Scanlines ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("ColorCycle", ColorCycle ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("MirrorHorizontal", MirrorHorizontal ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("MirrorVertical", MirrorVertical ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("Vignette", Vignette ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("RandomOrigin", RandomOrigin ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("OriginX", OriginX.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                key.SetValue("OriginY", OriginY.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                key.SetValue("MovingOrigin", MovingOrigin ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("Pixelation", Pixelation, RegistryValueKind.DWord);

                int i;
                for (i = 0; i < 4; i++)
                {
                    key.SetValue(
                        "Color" + (i + 1).ToString(CultureInfo.InvariantCulture),
                        unchecked((uint)Colors[i].ToArgb()).ToString("X8", CultureInfo.InvariantCulture),
                        RegistryValueKind.String);
                }
            }
        }

        private static double ReadDouble(RegistryKey key, string name, double fallback)
        {
            double value;
            string text = Convert.ToString(key.GetValue(name, fallback.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
            return Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : fallback;
        }

        private static bool ReadBool(RegistryKey key, string name, bool fallback)
        {
            object value = key.GetValue(name, fallback ? 1 : 0);
            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0;
            }
            catch
            {
                return fallback;
            }
        }

        private static int ReadInt(RegistryKey key, string name, int fallback)
        {
            object value = key.GetValue(name, fallback);
            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        private static Color ReadColor(RegistryKey key, string name, Color fallback)
        {
            string text = Convert.ToString(key.GetValue(name, String.Empty), CultureInfo.InvariantCulture);
            uint argb;
            if (UInt32.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out argb))
            {
                return Color.FromArgb(unchecked((int)argb));
            }
            return fallback;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}

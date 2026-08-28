using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PlasmaOldSchool
{
    internal sealed class PlasmaEngine : IDisposable
    {
        private const int SineTableSize = 4096;
        private const int SineTableMask = SineTableSize - 1;
        private const double InverseTwoPi = 1.0 / (Math.PI * 2.0);
        private static readonly double[] SineTable = BuildSineTable();

        private readonly PlasmaSettings _settings;
        private readonly int _width;
        private readonly int _height;
        private readonly Bitmap _frame;
        private readonly int[] _pixels;
        private readonly double[] _nx;
        private readonly double[] _ny;
        private readonly double[] _radius;
        private readonly double[] _angle;
        private readonly int[] _palette = new int[256];
        private readonly double _phaseA;
        private readonly double _phaseB;
        private readonly double _phaseC;
        private readonly double _seed;
        private readonly double _originX;
        private readonly double _originY;
        private double _time;
        private double _colorTime;
        private bool _gpuMode;

        internal PlasmaEngine(PlasmaSettings settings)
        {
            _settings = settings;
            _width = settings.RenderQuality == 1 ? 160 : (settings.RenderQuality == 3 ? 320 : 256);
            _height = settings.RenderQuality == 1 ? 90 : (settings.RenderQuality == 3 ? 180 : 144);
            _frame = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
            _pixels = new int[_width * _height];
            _nx = new double[_pixels.Length];
            _ny = new double[_pixels.Length];
            _radius = new double[_pixels.Length];
            _angle = new double[_pixels.Length];

            Random random = new Random(unchecked(Environment.TickCount * 397));
            _phaseA = random.NextDouble() * 7.0;
            _phaseB = random.NextDouble() * 7.0;
            _phaseC = random.NextDouble() * 7.0;
            _seed = random.NextDouble() * 20.0;
            _originX = settings.RandomOrigin ? 0.25 + random.NextDouble() * 0.5 : settings.OriginX;
            _originY = settings.RandomOrigin ? 0.25 + random.NextDouble() * 0.5 : settings.OriginY;
            _time = random.NextDouble() * 40.0;
            _colorTime = random.NextDouble() * 40.0;

            PrecomputeCoordinates();
            Render();
        }

        internal Bitmap Frame
        {
            get { return _frame; }
        }

        internal int Width { get { return _width; } }
        internal int Height { get { return _height; } }
        internal int TargetFps { get { return _settings.TargetFps; } }
        internal int ScanlineSpacing { get { return _settings.ScanlineSpacing; } }
        internal int ScanlineOpacity { get { return (int)(_settings.ScanlineOpacity * 2.55); } }
        internal bool MirrorHorizontal { get { return _settings.MirrorHorizontal; } }
        internal bool MirrorVertical { get { return _settings.MirrorVertical; } }
        internal bool Vignette { get { return _settings.Vignette; } }

        internal bool Scanlines
        {
            get { return _settings.Scanlines; }
        }

        internal double Time { get { return _time; } }
        internal double ColorShift { get { return _settings.ColorCycle ? (_colorTime * _settings.ColorCycleSpeed * 0.12) % 1.0 : 0.0; } }
        internal Color[] Colors { get { return _settings.Colors; } }
        internal double OriginX { get { return _originX; } }
        internal double OriginY { get { return _originY; } }
        internal double SpatialScale { get { return _settings.SpatialScale; } }
        internal double Warp { get { return _settings.Warp; } }
        internal double WaveDensity { get { return _settings.WaveDensity; } }
        internal double RadialPulse { get { return _settings.RadialPulse; } }
        internal double RotationSpeed { get { return _settings.RotationSpeed; } }
        internal double Brightness { get { return _settings.Brightness; } }
        internal double Contrast { get { return _settings.Contrast; } }
        internal bool ColorCycle { get { return _settings.ColorCycle; } }
        internal bool MovingOrigin { get { return _settings.MovingOrigin; } }
        internal int Pixelation { get { return _settings.Pixelation; } }
        internal double PhaseA { get { return _phaseA; } }
        internal double PhaseB { get { return _phaseB; } }
        internal double PhaseC { get { return _phaseC; } }
        internal double Seed { get { return _seed; } }

        internal void EnableGpuMode()
        {
            _gpuMode = true;
        }

        internal void Advance(double elapsedSeconds)
        {
            double elapsed = Math.Max(0.0, Math.Min(0.05, elapsedSeconds));
            _time += elapsed * _settings.MotionSpeed;
            _colorTime += elapsed;
            if (!_gpuMode)
            {
                Render();
            }
        }

        private void PrecomputeCoordinates()
        {
            double centerX = _width * _originX;
            double centerY = _height * _originY;
            int offset = 0;
            int y;
            int x;
            for (y = 0; y < _height; y++)
            {
                double normalizedY = (y - centerY) / _height;
                for (x = 0; x < _width; x++)
                {
                    double normalizedX = (x - centerX) / _height;
                    _nx[offset] = normalizedX;
                    _ny[offset] = normalizedY;
                    _radius[offset] = Math.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);
                    _angle[offset] = Math.Atan2(normalizedY, normalizedX);
                    offset++;
                }
            }
        }

        private void Render()
        {
            BuildPalette();
            double scale = _settings.SpatialScale;
            double warp = _settings.Warp;
            double waveDensity = _settings.WaveDensity;
            double time = _time;
            int pixelBlock = Math.Max(1, _settings.Pixelation);
            double orbitX = _settings.MovingOrigin ? Math.Sin(time * 0.5) * 50.0 * scale / _height : 0.0;
            double orbitY = _settings.MovingOrigin ? Math.Cos(time / 3.0) * 30.0 * scale / _height : 0.0;
            int y;
            for (y = 0; y < _height; y++)
            {
                int sampleY = Math.Min(_height - 1, (y / pixelBlock) * pixelBlock + pixelBlock / 2);
                int x;
                for (x = 0; x < _width; x++)
                {
                    int sampleX = Math.Min(_width - 1, (x / pixelBlock) * pixelBlock + pixelBlock / 2);
                    int sample = sampleY * _width + sampleX;
                    double radialX = _nx[sample] - orbitX;
                    double radialY = _ny[sample] - orbitY;
                    double radius = _settings.MovingOrigin ? Math.Sqrt(radialX * radialX + radialY * radialY) : _radius[sample];
                    double angle = _settings.MovingOrigin ? Math.Atan2(radialY, radialX) : _angle[sample];
                double waves =
                    FastSin(_nx[sample] * 12.5 * scale * waveDensity + time * 1.14 + _phaseA) +
                    FastSin(_ny[sample] * 14.0 * scale * waveDensity - time * 0.92 + _phaseB) +
                    FastSin((_nx[sample] + _ny[sample]) * 9.0 * scale * waveDensity + time * 0.73 + _phaseC) +
                    FastSin(radius * 24.0 * scale * waveDensity - time * 1.42 + _seed) * (1.05 + warp) * _settings.RadialPulse +
                    FastSin(angle * (2.0 + warp * 3.0) + radius * 11.0 * waveDensity - time * _settings.RotationSpeed) * warp;
                double normalized = 0.5 + 0.5 * FastSin(waves * 1.18 + time * 0.18);
                if (_settings.ColorCycle)
                {
                    // Recorre la propia paleta de forma circular: cada color
                    // se mezcla gradualmente con el siguiente y la vuelta
                    // completa vuelve al mismo punto sin salto.
                    normalized += ColorShift;
                    normalized -= Math.Floor(normalized);
                }
                int paletteIndex = Math.Max(0, Math.Min(255, (int)(normalized * 255.0)));
                    _pixels[y * _width + x] = _palette[paletteIndex];
                }
            }

            Rectangle rectangle = new Rectangle(0, 0, _width, _height);
            BitmapData data = _frame.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                Marshal.Copy(_pixels, 0, data.Scan0, _pixels.Length);
            }
            finally
            {
                _frame.UnlockBits(data);
            }
        }

        private void BuildPalette()
        {
            Color[] colors = _settings.Colors;

            int i;
            for (i = 0; i < 256; i++)
            {
                double position = (i / 256.0) * colors.Length;
                int first = ((int)Math.Floor(position)) % colors.Length;
                int second = (first + 1) % colors.Length;
                double fraction = position - Math.Floor(position);
                double blend = 0.5 - 0.5 * Math.Cos(fraction * Math.PI);
                double red = colors[first].R + (colors[second].R - colors[first].R) * blend;
                double green = colors[first].G + (colors[second].G - colors[first].G) * blend;
                double blue = colors[first].B + (colors[second].B - colors[first].B) * blend;

                red = (red - 128.0) * _settings.Contrast + 128.0;
                green = (green - 128.0) * _settings.Contrast + 128.0;
                blue = (blue - 128.0) * _settings.Contrast + 128.0;
                red *= _settings.Brightness;
                green *= _settings.Brightness;
                blue *= _settings.Brightness;

                int r = ClampByte(red);
                int g = ClampByte(green);
                int b = ClampByte(blue);
                _palette[i] = unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b;
            }
        }

        private static int ClampByte(double value)
        {
            return Math.Max(0, Math.Min(255, (int)value));
        }

        private static double[] BuildSineTable()
        {
            double[] table = new double[SineTableSize];
            int i;
            for (i = 0; i < table.Length; i++)
            {
                table[i] = Math.Sin(i * Math.PI * 2.0 / table.Length);
            }
            return table;
        }

        private static double FastSin(double radians)
        {
            int index = (int)(radians * InverseTwoPi * SineTableSize) & SineTableMask;
            return SineTable[index];
        }

        public void Dispose()
        {
            _frame.Dispose();
        }
    }
}

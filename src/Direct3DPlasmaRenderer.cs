using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PlasmaOldSchool
{
    internal sealed class Direct3DPlasmaRenderer : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct FrameData
        {
            public float ResolutionX, ResolutionY, Time, Scale;
            public float OriginX, OriginY, Warp, Density;
            public float Pulse, Rotation, Brightness, Contrast;
            public float PhaseA, PhaseB, PhaseC, ColorShift;
            public float C0R, C0G, C0B, C0A, C1R, C1G, C1B, C1A;
            public float C2R, C2G, C2B, C2A, C3R, C3G, C3B, C3A;
            public float MirrorX, MirrorY, PixelBlock, ScanlineSpacing;
            public float ScanlineOpacity, Seed;
            public int ColorCycle, MovingOrigin, Scanlines, Vignette;
            public float RenderScale, RgbPaletteTime;
            public int RgbPaletteCycle;
            public float Padding1, Padding2, Padding3;
        }

        private IntPtr _renderer;

        internal Direct3DPlasmaRenderer(Control control)
        {
            _renderer = Create(control.Handle);
            if (_renderer == IntPtr.Zero) throw new InvalidOperationException("Direct3D 11 no está disponible.");
        }

        internal bool Render(PlasmaEngine engine, int width, int height)
        {
            FrameData data = new FrameData();
            data.ResolutionX = width; data.ResolutionY = height; data.Time = (float)engine.Time; data.Scale = (float)engine.SpatialScale;
            data.OriginX = (float)engine.OriginX; data.OriginY = (float)engine.OriginY; data.Warp = (float)engine.Warp; data.Density = (float)engine.WaveDensity;
            data.Pulse = (float)engine.RadialPulse; data.Rotation = (float)engine.RotationSpeed; data.Brightness = (float)engine.Brightness; data.Contrast = (float)engine.Contrast;
            data.PhaseA = (float)engine.PhaseA; data.PhaseB = (float)engine.PhaseB; data.PhaseC = (float)engine.PhaseC; data.ColorShift = (float)engine.ColorShift;
            SetColor(engine.Colors[0], out data.C0R, out data.C0G, out data.C0B, out data.C0A); SetColor(engine.Colors[1], out data.C1R, out data.C1G, out data.C1B, out data.C1A);
            SetColor(engine.Colors[2], out data.C2R, out data.C2G, out data.C2B, out data.C2A); SetColor(engine.Colors[3], out data.C3R, out data.C3G, out data.C3B, out data.C3A);
            data.MirrorX = engine.MirrorHorizontal ? -1f : 1f; data.MirrorY = engine.MirrorVertical ? -1f : 1f; data.PixelBlock = engine.Pixelation; data.ScanlineSpacing = engine.ScanlineSpacing;
            data.ScanlineOpacity = engine.ScanlineOpacity / 255f; data.Seed = (float)engine.Seed; data.ColorCycle = engine.ColorCycle ? 1 : 0; data.MovingOrigin = engine.MovingOrigin ? 1 : 0; data.Scanlines = engine.Scanlines ? 1 : 0; data.Vignette = engine.Vignette ? 1 : 0;
            data.RenderScale = (float)engine.GpuRenderScale; data.RgbPaletteTime = (float)engine.RgbPaletteTime; data.RgbPaletteCycle = 0;
            return RenderNative(_renderer, Math.Max(1, width), Math.Max(1, height), ref data) != 0;
        }

        private static void SetColor(Color color, out float r, out float g, out float b, out float a) { r = color.R / 255f; g = color.G / 255f; b = color.B / 255f; a = 1f; }
        public void Dispose() { if (_renderer != IntPtr.Zero) { Destroy(_renderer); _renderer = IntPtr.Zero; } }

        [DllImport("PlasmaD3D11.dll", EntryPoint = "PlasmaD3D11_Create", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr Create(IntPtr window);
        [DllImport("PlasmaD3D11.dll", EntryPoint = "PlasmaD3D11_Render", CallingConvention = CallingConvention.Cdecl)] private static extern int RenderNative(IntPtr renderer, int width, int height, ref FrameData data);
        [DllImport("PlasmaD3D11.dll", EntryPoint = "PlasmaD3D11_Destroy", CallingConvention = CallingConvention.Cdecl)] private static extern void Destroy(IntPtr renderer);
    }
}

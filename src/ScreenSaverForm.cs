using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PlasmaOldSchool
{
    internal sealed class ScreenSaverForm : Form
    {
        private readonly PlasmaEngine _engine;
        private readonly bool _previewMode;
        private readonly IntPtr _previewParent;
        private readonly Action _requestExit;
        private GpuPlasmaRenderer _gpuRenderer;
        private Direct3DPlasmaRenderer _direct3DRenderer;
        private Point _initialMousePosition;
        private DateTime _shownAt;

        internal ScreenSaverForm(Rectangle bounds, PlasmaEngine engine, bool previewMode, IntPtr previewParent, Action requestExit)
        {
            _engine = engine;
            _previewMode = previewMode;
            _previewParent = previewParent;
            _requestExit = requestExit;

            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;
            Bounds = bounds;
            TopMost = !previewMode;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            if (_previewMode)
            {
                IntPtr windowHandle = Handle;
                NativeMethods.SetParent(windowHandle, _previewParent);
                int style = NativeMethods.GetWindowLong(windowHandle, NativeMethods.GwlStyle);
                style = (style | NativeMethods.WsChild) & ~NativeMethods.WsPopup;
                NativeMethods.SetWindowLong(windowHandle, NativeMethods.GwlStyle, style);
                SyncPreviewBounds();
            }
        }

        internal void SyncPreviewBounds()
        {
            if (!_previewMode || _previewParent == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.Rect rectangle;
            if (NativeMethods.GetClientRect(_previewParent, out rectangle))
            {
                Bounds = new Rectangle(0, 0, Math.Max(1, rectangle.Right - rectangle.Left), Math.Max(1, rectangle.Bottom - rectangle.Top));
            }
        }

        internal bool TryInitializeGpu()
        {
            if (_direct3DRenderer != null)
            {
                return true;
            }
            if (_gpuRenderer != null)
            {
                return true;
            }
            try
            {
                _direct3DRenderer = new Direct3DPlasmaRenderer(this);
                SetGpuPresentation(true);
                return true;
            }
            catch
            {
                if (_direct3DRenderer != null)
                {
                    _direct3DRenderer.Dispose();
                    _direct3DRenderer = null;
                }
            }
            return TryInitializeOpenGl();
        }

        private bool TryInitializeOpenGl()
        {
            if (_gpuRenderer != null) return true;
            try
            {
                _gpuRenderer = new GpuPlasmaRenderer(this);
                SetGpuPresentation(true);
                return true;
            }
            catch
            {
                if (_gpuRenderer != null)
                {
                    _gpuRenderer.Dispose();
                    _gpuRenderer = null;
                }
                SetGpuPresentation(false);
                return false;
            }
        }

        internal void DisableGpu()
        {
            if (_direct3DRenderer != null)
            {
                _direct3DRenderer.Dispose();
                _direct3DRenderer = null;
            }
            if (_gpuRenderer != null)
            {
                _gpuRenderer.Dispose();
                _gpuRenderer = null;
            }
            SetGpuPresentation(false);
        }

        private void SetGpuPresentation(bool enabled)
        {
            // Direct3D/OpenGL presentan directamente en la ventana. El búfer
            // de WinForms debe quedar desactivado o puede componerse después
            // de la GPU y mostrar durante un frame su superficie negra.
            DoubleBuffered = !enabled;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, !enabled);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            UpdateStyles();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _initialMousePosition = Cursor.Position;
            _shownAt = DateTime.UtcNow;
            if (!_previewMode)
            {
                Activate();
                Focus();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // El fotograma cubre toda la ventana; omitir el borrado evita parpadeos.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_direct3DRenderer != null)
            {
                if (_direct3DRenderer.Render(_engine, ClientSize.Width, ClientSize.Height)) return;
                _direct3DRenderer.Dispose();
                _direct3DRenderer = null;
                TryInitializeOpenGl();
            }
            if (_gpuRenderer != null)
            {
                _gpuRenderer.Render(_engine, ClientSize.Width, ClientSize.Height);
                return;
            }

            Graphics graphics = e.Graphics;
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.SmoothingMode = SmoothingMode.None;
            if (_engine.MirrorHorizontal || _engine.MirrorVertical)
            {
                graphics.TranslateTransform(_engine.MirrorHorizontal ? ClientSize.Width : 0, _engine.MirrorVertical ? ClientSize.Height : 0);
                graphics.ScaleTransform(_engine.MirrorHorizontal ? -1 : 1, _engine.MirrorVertical ? -1 : 1);
            }
            graphics.DrawImage(_engine.Frame, ClientRectangle);
            graphics.ResetTransform();

            if (_engine.Scanlines && ClientSize.Height > 10)
            {
                graphics.CompositingMode = CompositingMode.SourceOver;
                using (Pen scanline = new Pen(Color.FromArgb(_engine.ScanlineOpacity, 0, 0, 0), 1.0f))
                {
                    int y;
                    for (y = 2; y < ClientSize.Height; y += _engine.ScanlineSpacing)
                    {
                        graphics.DrawLine(scanline, 0, y, ClientSize.Width, y);
                    }
                }
            }

            if (_engine.Vignette && ClientSize.Width > 10 && ClientSize.Height > 10)
            {
                graphics.CompositingMode = CompositingMode.SourceOver;
                using (GraphicsPath path = new GraphicsPath())
                {
                    float diameter = (float)Math.Sqrt(ClientSize.Width * ClientSize.Width + ClientSize.Height * ClientSize.Height);
                    path.AddEllipse(
                        ClientSize.Width / 2.0f - diameter / 2.0f,
                        ClientSize.Height / 2.0f - diameter / 2.0f,
                        diameter,
                        diameter);
                    using (PathGradientBrush vignette = new PathGradientBrush(path))
                    {
                        vignette.CenterPoint = new PointF(ClientSize.Width / 2.0f, ClientSize.Height / 2.0f);
                        vignette.CenterColor = Color.FromArgb(0, 0, 0, 0);
                        vignette.SurroundColors = new[] { Color.FromArgb(155, 0, 0, 0) };
                        graphics.FillRectangle(vignette, ClientRectangle);
                    }
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            ExitIfFullScreen();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            ExitIfFullScreen();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_previewMode || DateTime.UtcNow.Subtract(_shownAt).TotalMilliseconds < 750.0)
            {
                return;
            }

            Point current = Cursor.Position;
            int dx = current.X - _initialMousePosition.X;
            int dy = current.Y - _initialMousePosition.Y;
            if (dx * dx + dy * dy > 36)
            {
                ExitIfFullScreen();
            }
        }

        private void ExitIfFullScreen()
        {
            if (!_previewMode && _requestExit != null)
            {
                _requestExit();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _gpuRenderer != null)
            {
                _gpuRenderer.Dispose();
                _gpuRenderer = null;
            }
            if (disposing && _direct3DRenderer != null)
            {
                _direct3DRenderer.Dispose();
                _direct3DRenderer = null;
            }
            base.Dispose(disposing);
        }
    }
}

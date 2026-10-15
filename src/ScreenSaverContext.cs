using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PlasmaOldSchool
{
    internal sealed class ScreenSaverContext : ApplicationContext
    {
        private readonly List<ScreenSaverForm> _forms = new List<ScreenSaverForm>();
        private readonly PlasmaEngine _engine;
        private readonly Timer _timer;
        private readonly Stopwatch _clock;
        private readonly bool _previewMode;
        private bool _exiting;
        private bool _cursorHidden;
        private Point _initialCursor;
        private bool _hasInitialCursor;
        private DateTime _startedAt;
        private bool _inputSettled;

        internal ScreenSaverContext(IntPtr previewParent)
        {
            _previewMode = previewParent != IntPtr.Zero;
            PlasmaSettings settings = PlasmaSettings.Load();
            _engine = new PlasmaEngine(settings);
            _clock = Stopwatch.StartNew();

            if (_previewMode)
            {
                ScreenSaverForm preview = new ScreenSaverForm(
                    new Rectangle(0, 0, 320, 180), _engine, true, previewParent, ExitSaver);
                AddForm(preview);
            }
            else
            {
                Screen[] screens = Screen.AllScreens;
                int i;
                for (i = 0; i < screens.Length; i++)
                {
                    AddForm(new ScreenSaverForm(screens[i].Bounds, _engine, false, IntPtr.Zero, ExitSaver));
                }
                Cursor.Hide();
                _cursorHidden = true;
            }

            bool gpuReady = true;
            int gpuIndex;
            for (gpuIndex = 0; gpuIndex < _forms.Count; gpuIndex++)
            {
                if (!_forms[gpuIndex].TryInitializeGpu())
                {
                    gpuReady = false;
                }
            }
            if (gpuReady)
            {
                _engine.EnableGpuMode();
                bool singleSurface = _forms.Count <= 1;
                int vsyncIndex;
                for (vsyncIndex = 0; vsyncIndex < _forms.Count; vsyncIndex++)
                {
                    _forms[vsyncIndex].SetVsyncWait(singleSurface);
                }
            }
            else
            {
                for (gpuIndex = 0; gpuIndex < _forms.Count; gpuIndex++)
                {
                    _forms[gpuIndex].DisableGpu();
                }
            }

            _startedAt = DateTime.UtcNow;
            _hasInitialCursor = !_previewMode && NativeMethods.TryGetCursorPosition(out _initialCursor);

            _timer = new Timer();
            _timer.Interval = Math.Max(10, Math.Min(100, 1000 / _engine.TargetFps));
            _timer.Tick += OnTick;
            _timer.Start();

            int formIndex;
            for (formIndex = 0; formIndex < _forms.Count; formIndex++)
            {
                _forms[formIndex].Show();
            }
        }

        private void AddForm(ScreenSaverForm form)
        {
            _forms.Add(form);
            form.FormClosed += OnFormClosed;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (ShouldExitFromGlobalInput())
            {
                ExitSaver();
                return;
            }

            double elapsed = _clock.Elapsed.TotalSeconds;
            _clock.Restart();
            _engine.Advance(elapsed);

            int i;
            for (i = 0; i < _forms.Count; i++)
            {
                if (_previewMode)
                {
                    _forms[i].SyncPreviewBounds();
                }
                _forms[i].Invalidate();
            }
        }

        // Se consulta el estado global porque con varias pantallas ninguna
        // ventana ve por si sola todo el teclado ni todo el recorrido del
        // raton. El margen inicial evita que el propio arranque lo cierre.
        private bool ShouldExitFromGlobalInput()
        {
            if (_previewMode || _exiting)
            {
                return false;
            }
            if (DateTime.UtcNow.Subtract(_startedAt).TotalMilliseconds < 750.0)
            {
                return false;
            }

            bool inputActive = NativeMethods.IsAnyInputKeyPressed() || NativeMethods.IsAnyMouseButtonPressed();

            // Windows puede lanzar el salvapantallas mientras el usuario aun
            // mantiene pulsada la tecla que agoto el tiempo de espera. Hasta
            // que todo deja de estar pulsado no se considera entrada nueva, o
            // se cerraria en el mismo instante de aparecer.
            if (!_inputSettled)
            {
                if (inputActive)
                {
                    return false;
                }
                _inputSettled = true;
                NativeMethods.TryGetCursorPosition(out _initialCursor);
            }

            if (inputActive)
            {
                return true;
            }

            Point current;
            if (_hasInitialCursor && NativeMethods.TryGetCursorPosition(out current))
            {
                int dx = current.X - _initialCursor.X;
                int dy = current.Y - _initialCursor.Y;
                if (dx * dx + dy * dy > 36)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            if (!_exiting)
            {
                ExitSaver();
            }
        }

        private void ExitSaver()
        {
            if (_exiting)
            {
                return;
            }
            _exiting = true;
            _timer.Stop();

            ScreenSaverForm[] forms = _forms.ToArray();
            int i;
            for (i = 0; i < forms.Length; i++)
            {
                forms[i].FormClosed -= OnFormClosed;
                forms[i].Close();
                forms[i].Dispose();
            }
            _forms.Clear();

            if (_cursorHidden)
            {
                Cursor.Show();
                _cursorHidden = false;
            }
            _engine.Dispose();
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_exiting)
            {
                ExitSaver();
            }
            if (disposing)
            {
                _timer.Dispose();
                _clock.Stop();
            }
            base.Dispose(disposing);
        }
    }
}

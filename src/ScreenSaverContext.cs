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
            }
            else
            {
                for (gpuIndex = 0; gpuIndex < _forms.Count; gpuIndex++)
                {
                    _forms[gpuIndex].DisableGpu();
                }
            }

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

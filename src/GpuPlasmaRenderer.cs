using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace PlasmaOldSchool
{
    // Renderizador OpenGL 2.0 sin dependencias externas. Si el driver no ofrece
    // shaders, ScreenSaverForm conserva el renderizador CPU como respaldo.
    internal sealed class GpuPlasmaRenderer : IDisposable
    {
        private const uint PfdDrawToWindow = 0x00000004;
        private const uint PfdSupportOpenGl = 0x00000020;
        private const uint PfdDoubleBuffer = 0x00000001;
        private const byte PfdTypeRgba = 0;
        private const byte PfdMainPlane = 0;
        private const uint GlColorBufferBit = 0x00004000;
        private const uint GlQuads = 0x0007;
        private const uint GlLines = 0x0001;
        private const uint GlBlend = 0x0BE2;
        private const uint GlSrcAlpha = 0x0302;
        private const uint GlOneMinusSrcAlpha = 0x0303;
        private const uint GlVertexShader = 0x8B31;
        private const uint GlFragmentShader = 0x8B30;
        private const uint GlCompileStatus = 0x8B81;
        private const uint GlLinkStatus = 0x8B82;
        private const uint GlTexture2D = 0x0DE1;
        private const uint GlRgba = 0x1908;
        private const uint GlUnsignedByte = 0x1401;
        private const uint GlTextureMinFilter = 0x2801;
        private const uint GlTextureMagFilter = 0x2800;
        private const int GlNearest = 0x2600;
        private const uint GlFramebuffer = 0x8D40;
        private const uint GlColorAttachment0 = 0x8CE0;
        private const uint GlFramebufferComplete = 0x8CD5;

        private readonly Control _control;
        private IntPtr _hdc;
        private IntPtr _glContext;
        private uint _program;
        private uint _vertexShader;
        private uint _fragmentShader;
        private uint _framebuffer;
        private uint _renderTexture;
        private int _renderWidth;
        private int _renderHeight;
        private bool _framebufferSupported;

        private GlCreateShader _createShader;
        private GlShaderSource _shaderSource;
        private GlCompileShader _compileShader;
        private GlGetShaderIv _getShaderIv;
        private GlGetShaderInfoLog _getShaderInfoLog;
        private GlCreateProgram _createProgram;
        private GlAttachShader _attachShader;
        private GlLinkProgram _linkProgram;
        private GlGetProgramIv _getProgramIv;
        private GlGetProgramInfoLog _getProgramInfoLog;
        private GlDeleteShader _deleteShader;
        private GlDeleteProgram _deleteProgram;
        private GlUseProgram _useProgram;
        private GlGetUniformLocation _getUniformLocation;
        private GlUniform1f _uniform1f;
        private GlUniform2f _uniform2f;
        private GlUniform3f _uniform3f;
        private GlUniform1i _uniform1i;
        private GlViewport _viewport;
        private GlClearColor _clearColor;
        private GlClear _clear;
        private GlGenFramebuffers _genFramebuffers;
        private GlBindFramebuffer _bindFramebuffer;
        private GlFramebufferTexture2D _framebufferTexture2D;
        private GlCheckFramebufferStatus _checkFramebufferStatus;
        private GlDeleteFramebuffers _deleteFramebuffers;

        private int _resolution;
        private int _time;
        private int _origin;
        private int _scale;
        private int _warp;
        private int _waveDensity;
        private int _radialPulse;
        private int _rotation;
        private int _brightness;
        private int _contrast;
        private int _phaseA;
        private int _phaseB;
        private int _phaseC;
        private int _seed;
        private int _colorCycle;
        private int _colorShift;
        private int _scanlines;
        private int _scanlineSpacing;
        private int _scanlineOpacity;
        private int _vignette;
        private int _pixelBlock;
        private int _movingOrigin;
        private int _mirror;
        private readonly int[] _colors = new int[4];

        internal GpuPlasmaRenderer(Control control)
        {
            _control = control;
            try
            {
                Initialize();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal void Render(PlasmaEngine engine, int width, int height)
        {
            if (_glContext == IntPtr.Zero)
            {
                return;
            }

            WglMakeCurrent(_hdc, _glContext);
            bool reducedRendering = EnsureRenderTarget(width, height, engine.GpuRenderScale);
            int renderWidth = reducedRendering ? _renderWidth : Math.Max(1, width);
            int renderHeight = reducedRendering ? _renderHeight : Math.Max(1, height);
            if (reducedRendering)
            {
                _bindFramebuffer(GlFramebuffer, _framebuffer);
            }
            _viewport(0, 0, renderWidth, renderHeight);
            _clearColor(0f, 0f, 0f, 1f);
            _clear(GlColorBufferBit);
            _useProgram(_program);

            _uniform2f(_resolution, renderWidth, renderHeight);
            _uniform1f(_time, (float)engine.Time);
            _uniform2f(_origin, (float)engine.OriginX, (float)engine.OriginY);
            _uniform1f(_scale, (float)engine.SpatialScale);
            _uniform1f(_warp, (float)engine.Warp);
            _uniform1f(_waveDensity, (float)engine.WaveDensity);
            _uniform1f(_radialPulse, (float)engine.RadialPulse);
            _uniform1f(_rotation, (float)engine.RotationSpeed);
            _uniform1f(_brightness, (float)engine.Brightness);
            _uniform1f(_contrast, (float)engine.Contrast);
            _uniform1f(_phaseA, (float)engine.PhaseA);
            _uniform1f(_phaseB, (float)engine.PhaseB);
            _uniform1f(_phaseC, (float)engine.PhaseC);
            _uniform1f(_seed, (float)engine.Seed);
            _uniform1f(_pixelBlock, engine.Pixelation);
            _uniform1i(_movingOrigin, engine.MovingOrigin ? 1 : 0);
            _uniform2f(_mirror, engine.MirrorHorizontal ? -1f : 1f, engine.MirrorVertical ? -1f : 1f);
            _uniform1i(_colorCycle, engine.ColorCycle ? 1 : 0);
            _uniform1f(_colorShift, (float)engine.ColorShift);
            _uniform1i(_scanlines, engine.Scanlines && !reducedRendering ? 1 : 0);
            _uniform1f(_scanlineSpacing, engine.ScanlineSpacing);
            _uniform1f(_scanlineOpacity, engine.ScanlineOpacity / 255f);
            _uniform1i(_vignette, engine.Vignette ? 1 : 0);

            int i;
            for (i = 0; i < 4; i++)
            {
                Color color = engine.Colors[i];
                _uniform3f(_colors[i], color.R / 255f, color.G / 255f, color.B / 255f);
            }

            GlBegin(GlQuads);
            GlVertex2f(-1f, -1f);
            GlVertex2f(1f, -1f);
            GlVertex2f(1f, 1f);
            GlVertex2f(-1f, 1f);
            GlEnd();
            _useProgram(0);
            if (reducedRendering)
            {
                _bindFramebuffer(GlFramebuffer, 0);
                _viewport(0, 0, Math.Max(1, width), Math.Max(1, height));
                GlEnable(GlTexture2D);
                GlBindTexture(GlTexture2D, _renderTexture);
                GlColor4f(1f, 1f, 1f, 1f);
                GlBegin(GlQuads);
                GlTexCoord2f(0f, 0f); GlVertex2f(-1f, -1f);
                GlTexCoord2f(1f, 0f); GlVertex2f(1f, -1f);
                GlTexCoord2f(1f, 1f); GlVertex2f(1f, 1f);
                GlTexCoord2f(0f, 1f); GlVertex2f(-1f, 1f);
                GlEnd();
                GlDisable(GlTexture2D);
                DrawScanlines(engine, width, height);
            }
            SwapBuffers(_hdc);
        }

        private static void DrawScanlines(PlasmaEngine engine, int width, int height)
        {
            if (!engine.Scanlines || height <= 10)
            {
                return;
            }

            GlEnable(GlBlend);
            GlBlendFunc(GlSrcAlpha, GlOneMinusSrcAlpha);
            GlColor4f(0f, 0f, 0f, engine.ScanlineOpacity / 255f);
            GlBegin(GlLines);
            int y;
            for (y = 2; y < height; y += engine.ScanlineSpacing)
            {
                float normalizedY = (y / (float)height) * 2f - 1f;
                GlVertex2f(-1f, normalizedY);
                GlVertex2f(1f, normalizedY);
            }
            GlEnd();
            GlDisable(GlBlend);
            GlColor4f(1f, 1f, 1f, 1f);
        }

        private void Initialize()
        {
            _hdc = GetDC(_control.Handle);
            if (_hdc == IntPtr.Zero)
            {
                throw new InvalidOperationException("No se pudo obtener el contexto gráfico de la ventana.");
            }

            PixelFormatDescriptor descriptor = new PixelFormatDescriptor();
            descriptor.Size = (ushort)Marshal.SizeOf(typeof(PixelFormatDescriptor));
            descriptor.Version = 1;
            descriptor.Flags = PfdDrawToWindow | PfdSupportOpenGl | PfdDoubleBuffer;
            descriptor.PixelType = PfdTypeRgba;
            descriptor.ColorBits = 32;
            descriptor.DepthBits = 16;
            descriptor.LayerType = PfdMainPlane;
            int format = ChoosePixelFormat(_hdc, ref descriptor);
            if (format == 0 || !SetPixelFormat(_hdc, format, ref descriptor))
            {
                throw new InvalidOperationException("La tarjeta gráfica no admite el formato OpenGL necesario.");
            }

            _glContext = WglCreateContext(_hdc);
            if (_glContext == IntPtr.Zero || !WglMakeCurrent(_hdc, _glContext))
            {
                throw new InvalidOperationException("No se pudo crear el contexto OpenGL.");
            }

            LoadFunctions();
            _program = BuildProgram(VertexShaderSource, FragmentShaderSource);
            _resolution = Uniform("uResolution");
            _time = Uniform("uTime");
            _origin = Uniform("uOrigin");
            _scale = Uniform("uScale");
            _warp = Uniform("uWarp");
            _waveDensity = Uniform("uWaveDensity");
            _radialPulse = Uniform("uRadialPulse");
            _rotation = Uniform("uRotation");
            _brightness = Uniform("uBrightness");
            _contrast = Uniform("uContrast");
            _phaseA = Uniform("uPhaseA");
            _phaseB = Uniform("uPhaseB");
            _phaseC = Uniform("uPhaseC");
            _seed = Uniform("uSeed");
            _colorCycle = Uniform("uColorCycle");
            _colorShift = Uniform("uColorShift");
            _scanlines = Uniform("uScanlines");
            _scanlineSpacing = Uniform("uScanlineSpacing");
            _scanlineOpacity = Uniform("uScanlineOpacity");
            _vignette = Uniform("uVignette");
            _pixelBlock = Uniform("uPixelBlock");
            _movingOrigin = Uniform("uMovingOrigin");
            _mirror = Uniform("uMirror");
            _colors[0] = Uniform("uColor0");
            _colors[1] = Uniform("uColor1");
            _colors[2] = Uniform("uColor2");
            _colors[3] = Uniform("uColor3");
        }

        private void LoadFunctions()
        {
            _createShader = Load<GlCreateShader>("glCreateShader");
            _shaderSource = Load<GlShaderSource>("glShaderSource");
            _compileShader = Load<GlCompileShader>("glCompileShader");
            _getShaderIv = Load<GlGetShaderIv>("glGetShaderiv");
            _getShaderInfoLog = Load<GlGetShaderInfoLog>("glGetShaderInfoLog");
            _createProgram = Load<GlCreateProgram>("glCreateProgram");
            _attachShader = Load<GlAttachShader>("glAttachShader");
            _linkProgram = Load<GlLinkProgram>("glLinkProgram");
            _getProgramIv = Load<GlGetProgramIv>("glGetProgramiv");
            _getProgramInfoLog = Load<GlGetProgramInfoLog>("glGetProgramInfoLog");
            _deleteShader = Load<GlDeleteShader>("glDeleteShader");
            _deleteProgram = Load<GlDeleteProgram>("glDeleteProgram");
            _useProgram = Load<GlUseProgram>("glUseProgram");
            _getUniformLocation = Load<GlGetUniformLocation>("glGetUniformLocation");
            _uniform1f = Load<GlUniform1f>("glUniform1f");
            _uniform2f = Load<GlUniform2f>("glUniform2f");
            _uniform3f = Load<GlUniform3f>("glUniform3f");
            _uniform1i = Load<GlUniform1i>("glUniform1i");
            _viewport = Load<GlViewport>("glViewport");
            _clearColor = Load<GlClearColor>("glClearColor");
            _clear = Load<GlClear>("glClear");
            _framebufferSupported = TryLoadFramebufferFunctions();
        }

        private bool TryLoadFramebufferFunctions()
        {
            try
            {
                _genFramebuffers = LoadExtension<GlGenFramebuffers>("glGenFramebuffers", "glGenFramebuffersEXT");
                _bindFramebuffer = LoadExtension<GlBindFramebuffer>("glBindFramebuffer", "glBindFramebufferEXT");
                _framebufferTexture2D = LoadExtension<GlFramebufferTexture2D>("glFramebufferTexture2D", "glFramebufferTexture2DEXT");
                _checkFramebufferStatus = LoadExtension<GlCheckFramebufferStatus>("glCheckFramebufferStatus", "glCheckFramebufferStatusEXT");
                _deleteFramebuffers = LoadExtension<GlDeleteFramebuffers>("glDeleteFramebuffers", "glDeleteFramebuffersEXT");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private T LoadExtension<T>(string coreName, string extensionName) where T : class
        {
            IntPtr pointer = WglGetProcAddress(coreName);
            if (pointer == IntPtr.Zero || pointer == new IntPtr(1) || pointer == new IntPtr(2) || pointer == new IntPtr(3) || pointer == new IntPtr(-1))
            {
                pointer = WglGetProcAddress(extensionName);
            }
            if (pointer == IntPtr.Zero || pointer == new IntPtr(1) || pointer == new IntPtr(2) || pointer == new IntPtr(3) || pointer == new IntPtr(-1))
            {
                throw new InvalidOperationException("El controlador OpenGL no expone " + coreName + ".");
            }
            return Marshal.GetDelegateForFunctionPointer(pointer, typeof(T)) as T;
        }

        private bool EnsureRenderTarget(int width, int height, double scale)
        {
            if (!_framebufferSupported)
            {
                return false;
            }

            int targetWidth = Math.Max(1, (int)Math.Ceiling(width * scale));
            int targetHeight = Math.Max(1, (int)Math.Ceiling(height * scale));
            if (_framebuffer != 0 && _renderWidth == targetWidth && _renderHeight == targetHeight)
            {
                return true;
            }

            ReleaseRenderTarget();
            _genFramebuffers(1, out _framebuffer);
            GlGenTextures(1, out _renderTexture);
            GlBindTexture(GlTexture2D, _renderTexture);
            GlTexParameteri(GlTexture2D, GlTextureMinFilter, GlNearest);
            GlTexParameteri(GlTexture2D, GlTextureMagFilter, GlNearest);
            GlTexImage2D(GlTexture2D, 0, (int)GlRgba, targetWidth, targetHeight, 0, GlRgba, GlUnsignedByte, IntPtr.Zero);
            _bindFramebuffer(GlFramebuffer, _framebuffer);
            _framebufferTexture2D(GlFramebuffer, GlColorAttachment0, GlTexture2D, _renderTexture, 0);
            bool complete = _checkFramebufferStatus(GlFramebuffer) == GlFramebufferComplete;
            _bindFramebuffer(GlFramebuffer, 0);
            if (!complete)
            {
                ReleaseRenderTarget();
                _framebufferSupported = false;
                return false;
            }

            _renderWidth = targetWidth;
            _renderHeight = targetHeight;
            return true;
        }

        private void ReleaseRenderTarget()
        {
            if (_framebuffer != 0 && _deleteFramebuffers != null)
            {
                _deleteFramebuffers(1, ref _framebuffer);
                _framebuffer = 0;
            }
            if (_renderTexture != 0)
            {
                GlDeleteTextures(1, ref _renderTexture);
                _renderTexture = 0;
            }
            _renderWidth = 0;
            _renderHeight = 0;
        }

        private T Load<T>(string name) where T : class
        {
            IntPtr pointer = WglGetProcAddress(name);
            if (pointer == IntPtr.Zero || pointer == new IntPtr(1) || pointer == new IntPtr(2) || pointer == new IntPtr(3) || pointer == new IntPtr(-1))
            {
                IntPtr module = GetModuleHandle("opengl32.dll");
                pointer = module == IntPtr.Zero ? IntPtr.Zero : GetProcAddress(module, name);
                if (pointer == IntPtr.Zero)
                {
                    throw new InvalidOperationException("El controlador OpenGL no expone " + name + ".");
                }
            }
            return Marshal.GetDelegateForFunctionPointer(pointer, typeof(T)) as T;
        }

        private uint BuildProgram(string vertexSource, string fragmentSource)
        {
            _vertexShader = CompileShader(GlVertexShader, vertexSource);
            _fragmentShader = CompileShader(GlFragmentShader, fragmentSource);
            uint program = _createProgram();
            _attachShader(program, _vertexShader);
            _attachShader(program, _fragmentShader);
            _linkProgram(program);
            int linked;
            _getProgramIv(program, GlLinkStatus, out linked);
            if (linked == 0)
            {
                throw new InvalidOperationException(ReadProgramLog(program));
            }
            return program;
        }

        private uint CompileShader(uint type, string source)
        {
            uint shader = _createShader(type);
            string[] sourceArray = { source };
            _shaderSource(shader, 1, sourceArray, IntPtr.Zero);
            _compileShader(shader);
            int compiled;
            _getShaderIv(shader, GlCompileStatus, out compiled);
            if (compiled == 0)
            {
                throw new InvalidOperationException(ReadShaderLog(shader));
            }
            return shader;
        }

        private int Uniform(string name)
        {
            return _getUniformLocation(_program, name);
        }

        private string ReadShaderLog(uint shader)
        {
            StringBuilder log = new StringBuilder(2048);
            int length;
            _getShaderInfoLog(shader, log.Capacity, out length, log);
            return log.ToString();
        }

        private string ReadProgramLog(uint program)
        {
            StringBuilder log = new StringBuilder(2048);
            int length;
            _getProgramInfoLog(program, log.Capacity, out length, log);
            return log.ToString();
        }

        public void Dispose()
        {
            if (_glContext != IntPtr.Zero)
            {
                WglMakeCurrent(_hdc, _glContext);
                ReleaseRenderTarget();
                if (_program != 0 && _deleteProgram != null) _deleteProgram(_program);
                if (_vertexShader != 0 && _deleteShader != null) _deleteShader(_vertexShader);
                if (_fragmentShader != 0 && _deleteShader != null) _deleteShader(_fragmentShader);
                WglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
                WglDeleteContext(_glContext);
                _glContext = IntPtr.Zero;
                _program = 0;
                _vertexShader = 0;
                _fragmentShader = 0;
            }
            if (_hdc != IntPtr.Zero)
            {
                ReleaseDC(_control.Handle, _hdc);
                _hdc = IntPtr.Zero;
            }
        }

        private const string VertexShaderSource = @"#version 120
void main() { gl_Position = gl_Vertex; }";

        private const string FragmentShaderSource = @"#version 120
uniform vec2 uResolution;
uniform float uTime, uScale, uWarp, uWaveDensity, uRadialPulse, uRotation;
uniform float uBrightness, uContrast, uPhaseA, uPhaseB, uPhaseC, uSeed;
uniform vec2 uOrigin, uMirror;
uniform int uColorCycle, uScanlines, uVignette;
uniform float uColorShift, uScanlineSpacing, uScanlineOpacity, uPixelBlock;
uniform int uMovingOrigin;
uniform vec3 uColor0, uColor1, uColor2, uColor3;

vec3 palette(float value) {
  float wrapped = fract(value);
  if (wrapped < 0.0) wrapped += 1.0;
  float position = wrapped * 4.0;
  int index = int(floor(position));
  float amount = smoothstep(0.0, 1.0, fract(position));
  if (index == 0) return mix(uColor0, uColor1, amount);
  if (index == 1) return mix(uColor1, uColor2, amount);
  if (index == 2) return mix(uColor2, uColor3, amount);
  return mix(uColor3, uColor0, amount);
}

void main() {
  vec2 screenCoord = gl_FragCoord.xy;
  if (uMirror.x < 0.0) screenCoord.x = uResolution.x - screenCoord.x;
  if (uMirror.y < 0.0) screenCoord.y = uResolution.y - screenCoord.y;
  vec2 uv = screenCoord / uResolution;
  vec2 fc = floor(screenCoord / uPixelBlock) * uPixelBlock + uPixelBlock * 0.5;
  float zoom = (160.0 / uResolution.x) * uScale;
  vec2 p = fc * zoom;
  vec2 center = uResolution * uOrigin * zoom;
  if (uMovingOrigin == 1) {
    center += vec2(sin(uTime * 0.5) * 50.0 * uScale,
                   cos(uTime / 3.0) * 30.0 * uScale);
  }

  float density = max(0.5, uWaveDensity);
  float v1 = 0.5 + 0.5 * sin(p.x * density / 16.0 + uPhaseA * 0.08);
  float v2 = 0.5 + 0.5 * sin(
      (p.x * sin(uTime * 0.5 * uRotation + uPhaseB * 0.04) +
       p.y * cos(uTime / max(0.5, 3.0 / max(0.1, uRotation)))) / (8.0 / density));
  vec2 eye = p - center;
  float v3 = 0.5 + 0.5 * sin(length(eye) / (8.0 / density) - uTime * 0.25);
  float v4 = 0.5 + 0.5 * sin(uTime + length(p) / (8.0 / density));
  float base = (v1 + v2 + mix(0.5, v3, clamp(uRadialPulse, 0.0, 1.5)) + v4) * 0.25;
  float extra = 0.5 + 0.5 * sin((p.x + p.y) / (11.0 / density) + uTime * 0.7 + uPhaseC);
  float value = mix(base, (base + extra) * 0.5, clamp(uWarp * 0.35, 0.0, 0.35));
  if (uColorCycle == 1) value += uColorShift;
  vec3 color = palette(value);
  color = (color - 0.5) * uContrast + 0.5;
  color *= uBrightness;
  if (uScanlines == 1) {
    float line = mod(screenCoord.y, uScanlineSpacing);
    if (line > uScanlineSpacing - 1.0) color *= 1.0 - uScanlineOpacity;
  }
  if (uVignette == 1) {
    float edge = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
    color *= mix(0.62, 1.0, smoothstep(0.0, 0.36, edge));
  }
  gl_FragColor = vec4(clamp(color, 0.0, 1.0), 1.0);
}";

        [StructLayout(LayoutKind.Sequential)]
        private struct PixelFormatDescriptor
        {
            public ushort Size;
            public ushort Version;
            public uint Flags;
            public byte PixelType;
            public byte ColorBits;
            public byte RedBits;
            public byte RedShift;
            public byte GreenBits;
            public byte GreenShift;
            public byte BlueBits;
            public byte BlueShift;
            public byte AlphaBits;
            public byte AlphaShift;
            public byte AccumBits;
            public byte AccumRedBits;
            public byte AccumGreenBits;
            public byte AccumBlueBits;
            public byte AccumAlphaBits;
            public byte DepthBits;
            public byte StencilBits;
            public byte AuxBuffers;
            public byte LayerType;
            public byte Reserved;
            public uint LayerMask;
            public uint VisibleMask;
            public uint DamageMask;
        }

        private delegate uint GlCreateShader(uint type);
        private delegate void GlShaderSource(uint shader, int count, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] source, IntPtr length);
        private delegate void GlCompileShader(uint shader);
        private delegate void GlGetShaderIv(uint shader, uint parameter, out int value);
        private delegate void GlGetShaderInfoLog(uint shader, int maxLength, out int length, StringBuilder log);
        private delegate uint GlCreateProgram();
        private delegate void GlAttachShader(uint program, uint shader);
        private delegate void GlLinkProgram(uint program);
        private delegate void GlGetProgramIv(uint program, uint parameter, out int value);
        private delegate void GlGetProgramInfoLog(uint program, int maxLength, out int length, StringBuilder log);
        private delegate void GlDeleteShader(uint shader);
        private delegate void GlDeleteProgram(uint program);
        private delegate void GlUseProgram(uint program);
        private delegate int GlGetUniformLocation(uint program, [MarshalAs(UnmanagedType.LPStr)] string name);
        private delegate void GlUniform1f(int location, float value);
        private delegate void GlUniform2f(int location, float first, float second);
        private delegate void GlUniform3f(int location, float first, float second, float third);
        private delegate void GlUniform1i(int location, int value);
        private delegate void GlViewport(int x, int y, int width, int height);
        private delegate void GlClearColor(float red, float green, float blue, float alpha);
        private delegate void GlClear(uint mask);
        private delegate void GlGenFramebuffers(int count, out uint framebuffers);
        private delegate void GlBindFramebuffer(uint target, uint framebuffer);
        private delegate void GlFramebufferTexture2D(uint target, uint attachment, uint textureTarget, uint texture, int level);
        private delegate uint GlCheckFramebufferStatus(uint target);
        private delegate void GlDeleteFramebuffers(int count, ref uint framebuffers);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")]
        private static extern int ChoosePixelFormat(IntPtr dc, ref PixelFormatDescriptor descriptor);
        [DllImport("gdi32.dll")]
        private static extern bool SetPixelFormat(IntPtr dc, int format, ref PixelFormatDescriptor descriptor);
        [DllImport("gdi32.dll")]
        private static extern bool SwapBuffers(IntPtr dc);
        [DllImport("opengl32.dll")]
        private static extern IntPtr wglCreateContext(IntPtr dc);
        [DllImport("opengl32.dll")]
        private static extern bool wglDeleteContext(IntPtr context);
        [DllImport("opengl32.dll")]
        private static extern bool wglMakeCurrent(IntPtr dc, IntPtr context);
        [DllImport("opengl32.dll")]
        private static extern IntPtr wglGetProcAddress(string name);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string moduleName);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr module, string procedureName);
        [DllImport("opengl32.dll")]
        private static extern void glBegin(uint mode);
        [DllImport("opengl32.dll")]
        private static extern void glEnd();
        [DllImport("opengl32.dll")]
        private static extern void glVertex2f(float x, float y);
        [DllImport("opengl32.dll")]
        private static extern void glTexCoord2f(float s, float t);
        [DllImport("opengl32.dll")]
        private static extern void glEnable(uint capability);
        [DllImport("opengl32.dll")]
        private static extern void glDisable(uint capability);
        [DllImport("opengl32.dll")]
        private static extern void glBindTexture(uint target, uint texture);
        [DllImport("opengl32.dll")]
        private static extern void glGenTextures(int count, out uint textures);
        [DllImport("opengl32.dll")]
        private static extern void glDeleteTextures(int count, ref uint textures);
        [DllImport("opengl32.dll")]
        private static extern void glTexParameteri(uint target, uint parameter, int value);
        [DllImport("opengl32.dll")]
        private static extern void glTexImage2D(uint target, int level, int internalFormat, int width, int height, int border, uint format, uint type, IntPtr pixels);
        [DllImport("opengl32.dll")]
        private static extern void glColor4f(float red, float green, float blue, float alpha);
        [DllImport("opengl32.dll")]
        private static extern void glBlendFunc(uint source, uint destination);

        private static T LoadStatic<T>(IntPtr pointer) where T : class
        {
            return Marshal.GetDelegateForFunctionPointer(pointer, typeof(T)) as T;
        }

        private static void GlBegin(uint mode) { glBegin(mode); }
        private static void GlEnd() { glEnd(); }
        private static void GlVertex2f(float x, float y) { glVertex2f(x, y); }
        private static void GlTexCoord2f(float s, float t) { glTexCoord2f(s, t); }
        private static void GlEnable(uint capability) { glEnable(capability); }
        private static void GlDisable(uint capability) { glDisable(capability); }
        private static void GlBindTexture(uint target, uint texture) { glBindTexture(target, texture); }
        private static void GlGenTextures(int count, out uint textures) { glGenTextures(count, out textures); }
        private static void GlDeleteTextures(int count, ref uint textures) { glDeleteTextures(count, ref textures); }
        private static void GlTexParameteri(uint target, uint parameter, int value) { glTexParameteri(target, parameter, value); }
        private static void GlTexImage2D(uint target, int level, int internalFormat, int width, int height, int border, uint format, uint type, IntPtr pixels) { glTexImage2D(target, level, internalFormat, width, height, border, format, type, pixels); }
        private static void GlColor4f(float red, float green, float blue, float alpha) { glColor4f(red, green, blue, alpha); }
        private static void GlBlendFunc(uint source, uint destination) { glBlendFunc(source, destination); }
        private static bool WglMakeCurrent(IntPtr dc, IntPtr context) { return wglMakeCurrent(dc, context); }
        private static IntPtr WglCreateContext(IntPtr dc) { return wglCreateContext(dc); }
        private static bool WglDeleteContext(IntPtr context) { return wglDeleteContext(context); }
        private static IntPtr WglGetProcAddress(string name) { return wglGetProcAddress(name); }
    }
}

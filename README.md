# Plasma Old School para Windows

Salvapantallas nativo de Windows inspirado en los plasmas de Amiga y la demoscene. Funciona sin conexión y no necesita instalar bibliotecas adicionales.

Cuando el controlador lo permite, el salvapantallas usa un shader OpenGL para calcular el plasma en la GPU. Si OpenGL 2.0 no está disponible, cambia automáticamente al renderizador CPU.

El shader usa cuatro ondas de estilo demoscene: bandas verticales, diagonal rotatoria, ojo circular orbital y pulso radial. Incluye pixelado por bloques y ciclo continuo de paleta para conservar el carácter del plasma web de referencia.

## Probarlo

Desde PowerShell, en esta carpeta:

```powershell
.\dist\PlasmaOldSchool.scr /s
```

El movimiento del ratón, un clic o cualquier tecla cierran el salvapantallas.

Para abrir su configuración:

```powershell
.\dist\PlasmaOldSchool.scr /c
```

## Instalarlo

1. Conserva `dist\PlasmaOldSchool.scr` en una ubicación estable.
2. Haz clic derecho sobre el archivo. En Windows 11 quizá debas elegir **Mostrar más opciones**.
3. Selecciona **Instalar** y elige **PlasmaOldSchool** en la configuración de salvapantallas de Windows.

El archivo admite los modos estándar que usa Windows:

- `/s`: pantalla completa en todos los monitores.
- `/c`: configuración de paleta y movimiento.
- `/p <HWND>`: previsualización integrada del Panel de control.

## Personalización

La ventana de configuración permite elegir presets retro —incluidos **Fuego**, **RGB spectrum**, **Bosque eléctrico**, **Océano profundo**, **Violeta neón**, **Terminal fósforo** y **Monocromo**—, editar sus cuatro colores y activar una evolución cromática gradual.

El motor también expone controles avanzados para densidad de ondas, pulso radial, giro, brillo, contraste, calidad de renderizado, FPS objetivo, separación y opacidad de scanlines, espejo horizontal/vertical, viñeta CRT y posición del origen. El origen puede aleatorizarse automáticamente en cada ejecución o fijarse con coordenadas X/Y.

El modo **Ahorro** está activado por defecto y limita la presentación a 30 FPS, reduciendo el consumo y el ruido de los ventiladores. Puede desactivarse desde la configuración si se prefiere priorizar fluidez.

Cuando se usa la GPU, la opción **Calidad** controla además la resolución interna del shader (25 %, 38 % o 50 % de la pantalla) y lo escala con píxel duro. Así conserva el acabado retro mientras reduce de forma notable el trabajo gráfico; los controladores que no admitan framebuffer objects conservan el renderizado GPU normal como respaldo.

La configuración se guarda para el usuario actual en `HKCU\Software\PlasmaOldSchoolScreenSaver`.

## Reconstruir

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

El script usa el compilador de .NET Framework incluido con Windows y genera `dist\PlasmaOldSchool.scr`.

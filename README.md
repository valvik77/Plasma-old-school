# Plasma Old School para Windows

Salvapantallas nativo de Windows inspirado en los plasmas de Amiga y la demoscene. Funciona sin conexión y no necesita instalar bibliotecas adicionales.

El salvapantallas prioriza un renderizador Direct3D 11 nativo, integrado de forma experimental para aprovechar mejor la pila gráfica de Windows. Si no está disponible o falla, cambia automáticamente a OpenGL y, como último respaldo, al renderizador CPU.

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

Ejecuta `release\PlasmaOldSchoolSetup.exe`. El instalador copia el paquete a `Program Files`, coloca únicamente el lanzador `.scr` en `System32` para que aparezca permanentemente en la lista de protectores de pantalla y añade accesos para configurarlo, probarlo o desinstalarlo. Solicita permisos de administrador porque Windows sólo enumera de forma fiable los `.scr` instalados en la carpeta del sistema.

Como alternativa manual, conserva la carpeta `dist` completa en una ubicación estable, haz clic derecho sobre `PlasmaOldSchool.scr` y selecciona **Instalar**. No copies únicamente el `.scr` si quieres conservar Direct3D 11 y la configuración WinUI 3.

El archivo admite los modos estándar que usa Windows:

- `/s`: pantalla completa en todos los monitores.
- `/c`: configuración de paleta y movimiento.
- `/p <HWND>`: previsualización integrada del Panel de control.

## Personalización

La ventana de configuración usa un diseño Fluent inspirado en WinUI: es redimensionable, adapta sus tarjetas al espacio disponible y usa el modo claro u oscuro del sistema. Permite elegir **Español** o **English**; la preferencia se guarda junto con el resto de la configuración. También permite elegir presets retro —incluidos **Fuego**, **RGB spectrum**, **Bosque eléctrico**, **Océano profundo**, **Violeta neón**, **Terminal fósforo** y **Monocromo**—, editar sus cuatro colores y activar una evolución cromática gradual.

El motor también expone controles avanzados para densidad de ondas, pulso radial, giro, brillo, contraste, calidad de renderizado, FPS objetivo, separación y opacidad de scanlines, espejo horizontal/vertical, viñeta CRT y posición del origen. El origen puede aleatorizarse automáticamente en cada ejecución o fijarse con coordenadas X/Y.

El modo **Ahorro** está activado por defecto y limita la presentación a 30 FPS, reduciendo el consumo y el ruido de los ventiladores. Puede desactivarse desde la configuración si se prefiere priorizar fluidez.

Cuando se usa la GPU, la opción **Calidad** controla además la resolución interna del shader (25 %, 38 % o 50 % de la pantalla) y lo escala con píxel duro. Tanto Direct3D 11 como OpenGL aplican esta reducción, conservando el acabado retro mientras disminuyen de forma notable el trabajo gráfico.

La configuración se guarda para el usuario actual en `HKCU\Software\PlasmaOldSchoolScreenSaver`.

## Reconstruir

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

El script usa .NET Framework para el salvapantallas, MSBuild para la configuración WinUI 3, las herramientas C++ de Visual Studio para `PlasmaD3D11.dll` e Inno Setup 7 para el instalador. Genera el paquete en `dist` y `release\PlasmaOldSchoolSetup.exe`. Los binarios públicos se generan sin una firma Authenticode personal.

Las comprobaciones internas pueden ejecutarse sobre `PlasmaOldSchool.exe`: `/test` valida la CPU, `/gputest` OpenGL y `/d3dtest` Direct3D 11. Un resultado correcto devuelve el código de salida 0.

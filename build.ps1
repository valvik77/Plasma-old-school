$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDirectory = Join-Path $projectRoot 'src'
$outputDirectory = Join-Path $projectRoot 'dist'
$executableFile = Join-Path $outputDirectory 'PlasmaOldSchool.exe'
$outputFile = Join-Path $outputDirectory 'PlasmaOldSchool.scr'
$iconFile = Join-Path $projectRoot 'assets\plasma-old-school.ico'
$configProject = Join-Path $projectRoot 'config-winui\PlasmaOldSchool.Config.csproj'
$configOutputDirectory = $outputDirectory

$compilerCandidates = @(
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe',
    'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if (-not $compiler) {
    throw 'No se encontró el compilador de .NET Framework incluido con Windows.'
}

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$sources = Get-ChildItem -LiteralPath $sourceDirectory -Filter '*.cs' | Sort-Object Name

$compilerArguments = @(
    '/nologo',
    '/optimize+',
    '/target:winexe',
    '/platform:anycpu',
    ('/out:' + $executableFile),
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll'
) + $sources.FullName

if (Test-Path -LiteralPath $iconFile) {
    $compilerArguments += ('/win32icon:' + $iconFile)
}

& $compiler $compilerArguments
if ($LASTEXITCODE -ne 0) {
    throw "La compilación terminó con el código $LASTEXITCODE."
}

Copy-Item -LiteralPath $executableFile -Destination $outputFile -Force

if (Test-Path -LiteralPath $configProject) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = $null
    if (Test-Path -LiteralPath $vswhere) {
        $visualStudioPath = & $vswhere -latest -products * -property installationPath
        if ($visualStudioPath) {
            $candidate = Join-Path $visualStudioPath 'MSBuild\Current\Bin\amd64\MSBuild.exe'
            if (Test-Path -LiteralPath $candidate) { $msbuild = $candidate }
        }
    }
    if (-not $msbuild) { throw 'No se encontró MSBuild de Visual Studio para compilar la configuración WinUI 3.' }

    & $msbuild $configProject /restore /p:Configuration=Release /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "La compilación WinUI 3 terminó con el código $LASTEXITCODE." }

    $configBuildOutput = Join-Path $projectRoot 'config-winui\bin\Release\net8.0-windows10.0.19041.0\win-x64'
    if (-not (Test-Path -LiteralPath $configBuildOutput)) { throw 'No se encontró la salida compilada de WinUI 3.' }
    New-Item -ItemType Directory -Force -Path $configOutputDirectory | Out-Null
    Copy-Item -Path (Join-Path $configBuildOutput '*') -Destination $configOutputDirectory -Recurse -Force
}
Write-Host "Creado: $outputFile"

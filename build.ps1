param(
    [switch]$NoInstaller
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDirectory = Join-Path $projectRoot 'src'
$outputDirectory = Join-Path $projectRoot 'dist'
$executableFile = Join-Path $outputDirectory 'PlasmaOldSchool.exe'
$outputFile = Join-Path $outputDirectory 'PlasmaOldSchool.scr'
$iconFile = Join-Path $projectRoot 'assets\plasma-old-school.ico'
$configProject = Join-Path $projectRoot 'config-winui\PlasmaOldSchool.Config.csproj'
$direct3DProject = Join-Path $projectRoot 'native-d3d11\PlasmaD3D11.vcxproj'
$installerScript = Join-Path $projectRoot 'installer\PlasmaOldSchool.iss'
$releaseDirectory = Join-Path $projectRoot 'release'
$configOutputDirectory = $outputDirectory

$compilerCandidates = @(
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe',
    'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if (-not $compiler) {
    throw 'No se encontró el compilador de .NET Framework incluido con Windows.'
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = $null
if (Test-Path -LiteralPath $vswhere) {
    $visualStudioPath = & $vswhere -latest -products * -property installationPath
    if ($visualStudioPath) {
        $msbuildCandidate = Join-Path $visualStudioPath 'MSBuild\Current\Bin\amd64\MSBuild.exe'
        if (Test-Path -LiteralPath $msbuildCandidate) { $msbuild = $msbuildCandidate }
    }
}

# dist es una salida generada. Recrearla evita conservar DLL, recursos o
# subcarpetas obsoletas de compilaciones anteriores.
if (Test-Path -LiteralPath $outputDirectory) {
    Remove-Item -LiteralPath $outputDirectory -Recurse -Force
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
    if (-not $msbuild) { throw 'No se encontró MSBuild de Visual Studio para compilar la configuración WinUI 3.' }

    & $msbuild $configProject /restore /p:Configuration=Release /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "La compilación WinUI 3 terminó con el código $LASTEXITCODE." }

    $configBuildOutput = Join-Path $projectRoot 'config-winui\bin\Release\net8.0-windows10.0.19041.0\win-x64'
    if (-not (Test-Path -LiteralPath $configBuildOutput)) { throw 'No se encontró la salida compilada de WinUI 3.' }
    New-Item -ItemType Directory -Force -Path $configOutputDirectory | Out-Null
    Copy-Item -Path (Join-Path $configBuildOutput '*') -Destination $configOutputDirectory -Recurse -Force
}

if (Test-Path -LiteralPath $direct3DProject) {
    if (-not $msbuild) { throw 'No se encontró MSBuild de Visual Studio para compilar el renderizador Direct3D 11.' }

    & $msbuild $direct3DProject /t:Build /p:Configuration=Release /p:Platform=x64 /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "La compilación Direct3D 11 terminó con el código $LASTEXITCODE." }

    $direct3DOutput = Join-Path $projectRoot 'native-d3d11\bin\Release\x64\PlasmaD3D11.dll'
    if (-not (Test-Path -LiteralPath $direct3DOutput)) { throw 'No se encontró la DLL compilada de Direct3D 11.' }
    Copy-Item -LiteralPath $direct3DOutput -Destination (Join-Path $outputDirectory 'PlasmaD3D11.dll') -Force
}

if (-not $NoInstaller -and (Test-Path -LiteralPath $installerScript)) {
    $innoCompiler = $null
    $innoCandidates = @(
        'C:\Program Files\Inno Setup 7\ISCC.exe',
        'C:\Program Files (x86)\Inno Setup 7\ISCC.exe',
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 7\ISCC.exe')
    )
    $innoCompiler = $innoCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $innoCompiler) {
        $uninstallRoots = @(
            'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
            'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
            'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*'
        )
        $innoInstall = Get-ItemProperty $uninstallRoots -ErrorAction SilentlyContinue |
            Where-Object { $_.DisplayName -like 'Inno Setup 7*' -and $_.InstallLocation } |
            Select-Object -First 1
        if ($innoInstall) {
            $innoCandidate = Join-Path $innoInstall.InstallLocation 'ISCC.exe'
            if (Test-Path -LiteralPath $innoCandidate) { $innoCompiler = $innoCandidate }
        }
    }
    if (-not $innoCompiler) { throw 'No se encontró el compilador de Inno Setup 7.' }

    New-Item -ItemType Directory -Force -Path $releaseDirectory | Out-Null
    & $innoCompiler '--no-signing' $installerScript
    if ($LASTEXITCODE -ne 0) { throw "La creación del instalador terminó con el código $LASTEXITCODE." }
}
Write-Host "Creado: $outputFile"
if (-not $NoInstaller -and (Test-Path -LiteralPath (Join-Path $releaseDirectory 'PlasmaOldSchoolSetup.exe'))) {
    Write-Host "Instalador: $(Join-Path $releaseDirectory 'PlasmaOldSchoolSetup.exe')"
}

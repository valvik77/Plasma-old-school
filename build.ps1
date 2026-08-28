$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDirectory = Join-Path $projectRoot 'src'
$outputDirectory = Join-Path $projectRoot 'dist'
$executableFile = Join-Path $outputDirectory 'PlasmaOldSchool.exe'
$outputFile = Join-Path $outputDirectory 'PlasmaOldSchool.scr'
$iconFile = Join-Path $projectRoot 'assets\plasma-old-school.ico'

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
Write-Host "Creado: $outputFile"

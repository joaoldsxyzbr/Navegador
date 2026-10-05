$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$versionPath = Join-Path $repoRoot "chromium\VERSION"
$seriesPath = Join-Path $repoRoot "chromium\patches\series"
$argsPath = Join-Path $repoRoot "chromium\args\windows-release.gn"
$legacyPath = Join-Path $repoRoot "src\Navegador"

if (-not (Test-Path $versionPath)) {
    throw "chromium/VERSION não existe."
}

$version = (Get-Content $versionPath -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw "Versão Chromium inválida: '$version'."
}

if (-not (Test-Path $argsPath)) {
    throw "Arquivo de argumentos Windows não existe."
}

if (-not (Test-Path $seriesPath)) {
    throw "chromium/patches/series não existe."
}

$patchRoot = Join-Path $repoRoot "chromium\patches"
$patches = Get-Content $seriesPath |
    ForEach-Object { $_.Trim() } |
    Where-Object { $_ -and -not $_.StartsWith("#") }

$seen = @{}
foreach ($patch in $patches) {
    if ($seen.ContainsKey($patch)) {
        throw "Patch duplicado em series: $patch"
    }

    $seen[$patch] = $true
    $patchPath = Join-Path $patchRoot $patch
    if (-not (Test-Path $patchPath)) {
        throw "Patch listado mas ausente: $patch"
    }
}

if (Test-Path $legacyPath) {
    throw "A implementação MAUI antiga reapareceu em src/Navegador."
}

Write-Host "Overlay válido."
Write-Host "Chromium: $version"
Write-Host "Patches: $($patches.Count)"

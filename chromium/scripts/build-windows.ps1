param(
    [Parameter()]
    [string]$Workspace = "C:\src\navegador-chromium"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$srcPath = Join-Path $Workspace "src"
$argsPath = Join-Path $repoRoot "chromium\args\windows-release.gn"

if (-not (Test-Path $srcPath)) {
    throw "Chromium não preparado em $srcPath. Execute prepare-windows.ps1 primeiro."
}

if (-not (Get-Command gn -ErrorAction SilentlyContinue)) {
    throw "Comando gn não encontrado. Confirme depot_tools no PATH."
}

if (-not (Get-Command autoninja -ErrorAction SilentlyContinue)) {
    throw "Comando autoninja não encontrado. Confirme depot_tools no PATH."
}

$gnArgs = (Get-Content $argsPath -Raw) -replace "\r?\n", " "

Push-Location $srcPath
try {
    & gn gen "out/Navegador" "--args=$gnArgs"
    if ($LASTEXITCODE -ne 0) {
        throw "gn gen falhou."
    }

    & autoninja -C "out/Navegador" chrome
    if ($LASTEXITCODE -ne 0) {
        throw "Build Chromium falhou."
    }

    Write-Host "Build concluído em $srcPath\out\Navegador."
}
finally {
    Pop-Location
}

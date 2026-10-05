param(
    [Parameter()]
    [string]$Workspace = "C:\src\navegador-chromium"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$version = (Get-Content (Join-Path $repoRoot "chromium\VERSION") -Raw).Trim()
$seriesPath = Join-Path $repoRoot "chromium\patches\series"
$patchRoot = Join-Path $repoRoot "chromium\patches"
$srcPath = Join-Path $Workspace "src"

if (-not (Get-Command fetch -ErrorAction SilentlyContinue)) {
    throw "depot_tools não foi encontrado no PATH (comando fetch)."
}

if (-not (Get-Command gclient -ErrorAction SilentlyContinue)) {
    throw "depot_tools não foi encontrado no PATH (comando gclient)."
}

New-Item -ItemType Directory -Force -Path $Workspace | Out-Null

if (-not (Test-Path $srcPath)) {
    Push-Location $Workspace
    try {
        & fetch chromium --no-history
        if ($LASTEXITCODE -ne 0) {
            throw "fetch chromium falhou."
        }
    }
    finally {
        Pop-Location
    }
}

Push-Location $srcPath
try {
    $dirty = (& git status --porcelain)
    if ($LASTEXITCODE -ne 0) {
        throw "Não foi possível inspecionar a árvore Chromium."
    }

    if ($dirty) {
        throw "A árvore Chromium possui alterações locais. Use uma árvore limpa antes de preparar o Navegador."
    }

    & git fetch origin "refs/tags/$version:refs/tags/$version"
    if ($LASTEXITCODE -ne 0) {
        throw "Não foi possível obter a tag Chromium $version."
    }

    & git checkout --detach $version
    if ($LASTEXITCODE -ne 0) {
        throw "Não foi possível posicionar o Chromium em $version."
    }

    Push-Location $Workspace
    try {
        & gclient sync -D
        if ($LASTEXITCODE -ne 0) {
            throw "gclient sync falhou."
        }
    }
    finally {
        Pop-Location
    }

    $patches = Get-Content $seriesPath |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ -and -not $_.StartsWith("#") }

    foreach ($patch in $patches) {
        $patchPath = Join-Path $patchRoot $patch
        & git apply --check $patchPath
        if ($LASTEXITCODE -ne 0) {
            throw "Patch incompatível com Chromium $version: $patch"
        }

        & git apply $patchPath
        if ($LASTEXITCODE -ne 0) {
            throw "Falha ao aplicar patch: $patch"
        }
    }

    Write-Host "Chromium $version preparado em $srcPath."
    Write-Host "$($patches.Count) patch(es) aplicado(s)."
}
finally {
    Pop-Location
}

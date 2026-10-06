#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Version,
    [switch]$SkipTests,
    [switch]$Push
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $root 'VERSION'
$tag = "v$Version"

if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Use o formato X.Y.Z. Recebido: '$Version'." }

Push-Location $root
try {
    if (git status --porcelain) { throw 'Há alterações não commitadas.' }
    git rev-parse -q --verify "refs/tags/$tag" | Out-Null
    if ($LASTEXITCODE -eq 0) { throw "A etiqueta $tag já existe." }

    if (-not $SkipTests) {
        dotnet run --project desktop/Navegador.Tests/Navegador.Tests.csproj --configuration Release
        if ($LASTEXITCODE -ne 0) { throw 'Os testes falharam; release abortada.' }
    }

    $current = (Get-Content $versionFile -Raw).Trim()
    if ($current -ne $Version) {
        Set-Content -Path $versionFile -Value ($Version + [Environment]::NewLine) -NoNewline -Encoding utf8
        git add VERSION
        git commit -m "release: $tag"
    }
    else {
        Write-Host "VERSION já está em $Version; criando a tag no commit atual." -ForegroundColor Cyan
    }

    git tag -a $tag -m "Rumo $Version"
    if ($Push) {
        git push origin main
        git push origin $tag
    }
    else {
        Write-Host "Para publicar: git push origin main; git push origin $tag" -ForegroundColor Yellow
    }
}
finally { Pop-Location }

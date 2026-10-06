#!/usr/bin/env pwsh
#
# Publica uma versão do Navegador.
#
#   pwsh scripts/release.ps1 0.5.0
#
# O script escreve a versão no arquivo VERSION, roda os testes, faz o commit e
# cria a etiqueta. O workflow do GitHub Actions pega a partir do push da
# etiqueta: a versão do pacote vem sempre do arquivo VERSION.

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

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Use o formato X.Y.Z (por exemplo 0.5.0). Recebido: '$Version'."
}

Push-Location $root
try {
    $current = (Get-Content $versionFile -Raw).Trim()
    if ($current -eq $Version) {
        throw "O arquivo VERSION já está em $Version."
    }

    if (git status --porcelain) {
        throw 'Há alterações não commitadas. Faça o commit ou o stash antes de publicar.'
    }

    git rev-parse -q --verify "refs/tags/$tag" | Out-Null
    if ($LASTEXITCODE -eq 0) {
        throw "A etiqueta $tag já existe."
    }

    if (-not $SkipTests) {
        Write-Host 'Rodando os testes...' -ForegroundColor Cyan
        dotnet run --project desktop/Navegador.Tests/Navegador.Tests.csproj --configuration Release
        if ($LASTEXITCODE -ne 0) { throw 'Os testes falharam; a release foi abortada.' }
    }

    Set-Content -Path $versionFile -Value "$Version`n" -NoNewline -Encoding utf8
    Write-Host "VERSION: $current -> $Version" -ForegroundColor Green

    git add VERSION
    git commit -m "release: $tag"
    git tag -a $tag -m "Navegador $Version"

    Write-Host ''
    Write-Host "Commit e etiqueta $tag criados." -ForegroundColor Green

    if ($Push) {
        Write-Host 'Enviando para o origin...' -ForegroundColor Cyan
        git push origin main
        git push origin $tag
    }
    else {
        Write-Host 'Para publicar, rode:' -ForegroundColor Yellow
        Write-Host "  git push origin main; git push origin $tag"
    }
}
finally {
    Pop-Location
}

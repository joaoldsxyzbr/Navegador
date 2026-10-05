param(
    [Parameter(Mandatory = $true)]
    [string]$Package,
    [Parameter(Mandatory = $true)]
    [string]$Root,
    [Parameter(Mandatory = $true)]
    [string]$ExpectedVersion,
    [Parameter(Mandatory = $true)]
    [int]$UpdaterPid
)

$ErrorActionPreference = "Stop"
$Root = [IO.Path]::GetFullPath($Root)
$Package = [IO.Path]::GetFullPath($Package)
$appRoot = [IO.Path]::GetFullPath((Join-Path $Root "App"))
$stage = Join-Path $env:TEMP ("Navegador-stage-" + [Guid]::NewGuid().ToString("N"))
$backup = Join-Path $Root ".update-backup"
$managed = @("App","Navegador.exe","Atualizar Navegador.exe","Updater","version.txt","NOTICE.txt")

function Stop-NavegadorProcesses {
    $processes = Get-CimInstance Win32_Process -Filter "Name = 'chrome.exe'" -ErrorAction SilentlyContinue
    foreach ($process in $processes) {
        $path = $process.ExecutablePath
        if ([string]::IsNullOrWhiteSpace($path)) { continue }
        try {
            $full = [IO.Path]::GetFullPath($path)
            if ($full.StartsWith($appRoot, [StringComparison]::OrdinalIgnoreCase)) {
                Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
            }
        } catch {}
    }
}

try {
    Wait-Process -Id $UpdaterPid -Timeout 30 -ErrorAction SilentlyContinue
    Stop-NavegadorProcesses
    Start-Sleep -Milliseconds 700

    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory -Path $stage | Out-Null
    Expand-Archive -LiteralPath $Package -DestinationPath $stage -Force

    $newChrome = Join-Path $stage "App\chrome.exe"
    $newVersionFile = Join-Path $stage "version.txt"
    if (-not (Test-Path $newChrome)) { throw "Pacote inválido: App\chrome.exe ausente." }
    if (-not (Test-Path $newVersionFile)) { throw "Pacote inválido: version.txt ausente." }

    $newVersion = (Get-Content $newVersionFile -Raw).Trim()
    if ($newVersion -ne $ExpectedVersion) {
        throw "Versão do pacote '$newVersion' não corresponde ao manifesto '$ExpectedVersion'."
    }

    if (Test-Path $backup) { Remove-Item $backup -Recurse -Force }
    New-Item -ItemType Directory -Path $backup | Out-Null

    foreach ($item in $managed) {
        $current = Join-Path $Root $item
        if (Test-Path $current) {
            Move-Item -LiteralPath $current -Destination (Join-Path $backup $item) -Force
        }
    }

    try {
        foreach ($item in $managed) {
            $incoming = Join-Path $stage $item
            if (Test-Path $incoming) {
                Move-Item -LiteralPath $incoming -Destination (Join-Path $Root $item) -Force
            }
        }

        if (-not (Test-Path (Join-Path $Root "App\chrome.exe"))) {
            throw "Atualização incompleta: Chromium ausente após a troca."
        }

        if (-not (Test-Path (Join-Path $Root "Data"))) {
            New-Item -ItemType Directory -Path (Join-Path $Root "Data") | Out-Null
        }

        Remove-Item $backup -Recurse -Force
    }
    catch {
        foreach ($item in $managed) {
            $failed = Join-Path $Root $item
            if (Test-Path $failed) { Remove-Item $failed -Recurse -Force }
            $previous = Join-Path $backup $item
            if (Test-Path $previous) {
                Move-Item -LiteralPath $previous -Destination $failed -Force
            }
        }
        throw
    }

    Remove-Item $Package -Force -ErrorAction SilentlyContinue
    Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
    Start-Process -FilePath (Join-Path $Root "Navegador.exe") -WorkingDirectory $Root
}
catch {
    $message = "A atualização não pôde ser concluída." + [Environment]::NewLine + [Environment]::NewLine + $_.Exception.Message
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show($message, "Atualizador do Navegador", "OK", "Error") | Out-Null
    exit 1
}

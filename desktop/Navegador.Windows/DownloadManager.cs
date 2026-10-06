using Microsoft.Web.WebView2.Core;
using Navegador.Core.Storage;

namespace Navegador.Windows;

/// <summary>Uma transferência em andamento ou recente.</summary>
internal sealed class DownloadItem
{
    private const int StaleAfterMinutes = 5;

    public DownloadItem(CoreWebView2DownloadOperation operation, string fileName)
    {
        Operation = operation;
        FileName = fileName;
        StartedAt = DateTimeOffset.Now;
    }

    public CoreWebView2DownloadOperation Operation { get; }

    public string FileName { get; }

    public DateTimeOffset StartedAt { get; }

    public string Url => Operation.Uri;

    public string DisplayHost
    {
        get
        {
            var host = Navegador.Core.AddressResolver.HostOf(Url);

            if (!string.IsNullOrEmpty(host)) return host;

            // Nunca mostre a URL crua: ela pode conter credenciais ou tokens.
            return "arquivo local";
        }
    }

    public string? ResultFilePath => Operation.ResultFilePath;

    public CoreWebView2DownloadState State => Operation.State;

    public bool IsRunning => State == CoreWebView2DownloadState.InProgress;

    public bool IsInterrupted => State == CoreWebView2DownloadState.Interrupted;

    public bool IsComplete => State == CoreWebView2DownloadState.Completed;

    public ulong? TotalBytes => Operation.TotalBytesToReceive;

    public ulong ReceivedBytes => Operation.BytesReceived <= 0
        ? 0UL
        : (ulong)Operation.BytesReceived;

    public int Progress => TotalBytes is > 0
        ? (int)Math.Clamp(Math.Round(ReceivedBytes * 100d / TotalBytes.Value), 0d, 100d)
        : 0;

    public string SizeText => TotalBytes is > 0
        ? $"{Format(ReceivedBytes)} de {Format(TotalBytes.Value)}"
        : $"{Format(ReceivedBytes)} recebidos";

    public string StatusText => State switch
    {
        CoreWebView2DownloadState.InProgress => $"{Progress}% — {SizeText}",
        CoreWebView2DownloadState.Completed => $"Concluído — {Format(ReceivedBytes)}",
        CoreWebView2DownloadState.Interrupted => $"Interrompido — {InterruptReasonText(Operation.InterruptReason)}",
        _ => SizeText
    };

    public bool IsStale => IsComplete && DateTimeOffset.Now - StartedAt > TimeSpan.FromMinutes(StaleAfterMinutes);

    private static string InterruptReasonText(CoreWebView2DownloadInterruptReason reason) => reason switch
    {
        CoreWebView2DownloadInterruptReason.None => "sem motivo informado",
        CoreWebView2DownloadInterruptReason.FileFailed => "falha ao gravar o arquivo",
        CoreWebView2DownloadInterruptReason.FileAccessDenied => "sem permissão na pasta de destino",
        CoreWebView2DownloadInterruptReason.FileNoSpace => "disco cheio",
        CoreWebView2DownloadInterruptReason.FileNameTooLong => "nome do arquivo muito longo",
        CoreWebView2DownloadInterruptReason.FileTooLarge => "arquivo grande demais para o sistema de arquivos",
        CoreWebView2DownloadInterruptReason.FileMalicious => "arquivo bloqueado pela proteção do Windows",
        CoreWebView2DownloadInterruptReason.FileTransientError => "arquivo temporariamente indisponível",
        CoreWebView2DownloadInterruptReason.FileBlockedByPolicy => "arquivo bloqueado por política",
        CoreWebView2DownloadInterruptReason.FileSecurityCheckFailed => "falha na verificação de segurança",
        CoreWebView2DownloadInterruptReason.FileTooShort => "arquivo parcial reiniciado",
        CoreWebView2DownloadInterruptReason.FileHashMismatch => "arquivo parcial inválido",
        CoreWebView2DownloadInterruptReason.NetworkFailed => "falha de rede",
        CoreWebView2DownloadInterruptReason.NetworkTimeout => "tempo de rede esgotado",
        CoreWebView2DownloadInterruptReason.NetworkDisconnected => "conexão perdida",
        CoreWebView2DownloadInterruptReason.NetworkServerDown => "servidor indisponível",
        CoreWebView2DownloadInterruptReason.NetworkInvalidRequest => "requisição de rede inválida",
        CoreWebView2DownloadInterruptReason.ServerFailed => "falha no servidor",
        CoreWebView2DownloadInterruptReason.ServerNoRange => "servidor não permite retomar",
        CoreWebView2DownloadInterruptReason.ServerBadContent => "conteúdo indisponível",
        CoreWebView2DownloadInterruptReason.ServerUnauthorized => "download não autorizado",
        CoreWebView2DownloadInterruptReason.ServerCertificateProblem => "problema no certificado do servidor",
        CoreWebView2DownloadInterruptReason.ServerForbidden => "download proibido",
        CoreWebView2DownloadInterruptReason.ServerUnexpectedResponse => "resposta inesperada do servidor",
        CoreWebView2DownloadInterruptReason.ServerContentLengthMismatch => "tamanho recebido diferente do informado",
        CoreWebView2DownloadInterruptReason.ServerCrossOriginRedirect => "redirecionamento inesperado",
        CoreWebView2DownloadInterruptReason.UserCanceled => "cancelado",
        CoreWebView2DownloadInterruptReason.UserShutdown => "cancelado ao fechar",
        CoreWebView2DownloadInterruptReason.UserPaused => "pausado",
        CoreWebView2DownloadInterruptReason.DownloadProcessCrashed => "processo de download encerrou",
        _ => reason.ToString()
    };

    private static string Format(ulong bytes)
    {
        if (bytes <= 0) return "0 B";

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{bytes} {units[unit]}"
            : $"{value:0.#} {units[unit]}";
    }
}

/// <summary>
/// Mantém a lista de downloads e o painel que aparece na parte de baixo da
/// janela. Sem isso o WebView2 cancela cada download silenciosamente, porque
/// nenhum destino foi escolhido.
/// </summary>
internal sealed class DownloadManager : IDisposable
{
    private const int MaxItems = 100;

    private readonly List<DownloadItem> _items = [];
    private readonly SettingsStore _settings;
    private readonly System.Windows.Forms.Timer _cleanupTimer;

    public DownloadManager(SettingsStore settings)
    {
        _settings = settings;

        _cleanupTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
        _cleanupTimer.Tick += (_, _) =>
        {
            if (RemoveStale()) NotifyChanged();
        };
        _cleanupTimer.Start();
    }

    /// <summary>Disparado quando a lista ou o progresso mudam.</summary>
    public event EventHandler? RefreshRequested;

    public IReadOnlyList<DownloadItem> Items => _items;

    public bool HasRunningDownloads => _items.Any(item => item.IsRunning);

    private void NotifyChanged() => RefreshRequested?.Invoke(this, EventArgs.Empty);

    public void Begin(CoreWebView2DownloadStartingEventArgs eventArgs)
    {
        var operation = eventArgs.DownloadOperation;

        // A pasta é decidida pelo Navegador, então o diálogo do WebView2 não aparece.
        eventArgs.Handled = true;

        var fileName = ResolveFileName(operation, eventArgs.ResultFilePath);
        var folder = _settings.ResolveDownloadFolder();

        if (_settings.Current.AskWhereToSaveDownloads)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Salvar download",
                FileName = fileName,
                InitialDirectory = Directory.Exists(folder) ? folder : null,
                Filter = "Todos os arquivos (*.*)|*.*",
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                eventArgs.Cancel = true;
                return;
            }

            eventArgs.ResultFilePath = dialog.FileName;
        }
        else
        {
            try
            {
                Directory.CreateDirectory(folder);
                eventArgs.ResultFilePath = UniqueFilePath(folder, fileName);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                eventArgs.Cancel = true;
                MessageBox.Show(
                    $"Não foi possível iniciar o download em:\n{folder}\n\n{exception.Message}\n\n" +
                    "Escolha outra pasta em Menu › Configurações.",
                    "Downloads",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
        }

        var item = new DownloadItem(operation, Path.GetFileName(eventArgs.ResultFilePath));
        _items.Insert(0, item);
        Trim();

        operation.BytesReceivedChanged += (_, _) => NotifyChanged();
        operation.StateChanged += (_, _) =>
        {
            if (item.IsComplete) RemoveStale();
            NotifyChanged();
        };

        NotifyChanged();
    }

    public void Cancel(DownloadItem item)
    {
        try
        {
            if (item.IsRunning) item.Operation.Cancel();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            // A transferência pode ter terminado entre o clique e a chamada.
        }

        NotifyChanged();
    }

    public void RemoveCompleted(DownloadItem item)
    {
        if (item.IsRunning) return;

        _items.Remove(item);
        NotifyChanged();
    }

    public void ClearCompleted()
    {
        _items.RemoveAll(item => !item.IsRunning);
        NotifyChanged();
    }

    public void Dispose() => _cleanupTimer.Dispose();

    private bool RemoveStale() => _items.RemoveAll(item => item.IsStale) > 0;

    private void Trim()
    {
        if (_items.Count <= MaxItems) return;

        // Só remove o que já terminou; um download em curso nunca é descartado.
        for (var index = _items.Count - 1; index >= 0 && _items.Count > MaxItems; index--)
        {
            if (!_items[index].IsRunning) _items.RemoveAt(index);
        }
    }

    private static string ResolveFileName(CoreWebView2DownloadOperation operation, string proposedPath)
    {
        // Preferência: nome sugerido pela operação; depois o último segmento da URL.
        var fromResult = Path.GetFileName(operation.ResultFilePath);
        if (IsUsableName(fromResult)) return fromResult;

        var fromUri = FileNameFromUrl(operation.Uri);
        if (IsUsableName(fromUri)) return fromUri!;

        var fromProposal = Path.GetFileName(proposedPath);
        if (IsUsableName(fromProposal)) return fromProposal;

        return "download";
    }

    private static string? FileNameFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

        var last = uri.Segments.LastOrDefault();
        if (string.IsNullOrWhiteSpace(last)) return null;

        var trimmed = last.TrimEnd('/');
        return string.IsNullOrWhiteSpace(trimmed) ? null : Uri.UnescapeDataString(trimmed);
    }

    private static bool IsUsableName(string? name) =>
        !string.IsNullOrWhiteSpace(name) &&
        name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
        name is not "." and not "..";

    private static string UniqueFilePath(string folder, string fileName)
    {
        var candidate = Path.Combine(folder, fileName);
        if (!File.Exists(candidate)) return candidate;

        var stem = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        for (var index = 1; index < 10_000; index++)
        {
            candidate = Path.Combine(folder, $"{stem} ({index}){extension}");
            if (!File.Exists(candidate)) return candidate;
        }

        return Path.Combine(folder, $"{stem}-{Guid.NewGuid():N}{extension}");
    }
}

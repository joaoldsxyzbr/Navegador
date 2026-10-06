using CefSharp;
using CefSharp.Handler;
using Navegador.Core.Storage;

namespace Navegador.Windows;

/// <summary>Uma transferência em andamento ou recente.</summary>
internal sealed class DownloadItem
{
    private const int StaleAfterMinutes = 5;
    private CefSharp.DownloadItem _source;

    public DownloadItem(CefSharp.DownloadItem source, string? targetFilePath = null)
    {
        _source = source;
        TargetFilePath = targetFilePath;
        StartedAt = source.StartTime is { } start ? new DateTimeOffset(start) : DateTimeOffset.Now;
        Refresh(source, targetFilePath);
    }

    public int Id => _source.Id;

    public string FileName { get; private set; } = "download";

    public DateTimeOffset StartedAt { get; }

    public string Url => _source.Url ?? string.Empty;

    public string DisplayHost
    {
        get
        {
            var host = Navegador.Core.AddressResolver.HostOf(Url);
            return !string.IsNullOrEmpty(host) ? host : "arquivo local";
        }
    }

    public string? TargetFilePath { get; private set; }

    public string? ResultFilePath =>
        !string.IsNullOrWhiteSpace(_source.FullPath) ? _source.FullPath : TargetFilePath;

    public bool IsRunning => _source.IsInProgress;

    public bool IsInterrupted => _source.IsInterrupted || _source.IsCancelled;

    public bool IsComplete => _source.IsComplete;

    public long TotalBytes => _source.TotalBytes;

    public long ReceivedBytes => _source.ReceivedBytes;

    public int Progress => _source.PercentComplete >= 0
        ? Math.Clamp(_source.PercentComplete, 0, 100)
        : TotalBytes > 0
            ? (int)Math.Clamp(Math.Round(ReceivedBytes * 100d / TotalBytes), 0d, 100d)
            : 0;

    public string StatusText => IsRunning
        ? $"{(Progress > 0 ? $"{Progress}% — " : string.Empty)}{SizeText}"
        : IsComplete
            ? $"Concluído — {Format(ReceivedBytes)}"
            : IsInterrupted
                ? $"Interrompido — {_source.InterruptReason}"
                : SizeText;

    public bool IsStale => IsComplete && DateTimeOffset.Now - StartedAt > TimeSpan.FromMinutes(StaleAfterMinutes);

    internal IDownloadItemCallback? Callback { get; set; }

    internal void ReplaceCallback(IDownloadItemCallback callback)
    {
        if (ReferenceEquals(Callback, callback)) return;
        Callback?.Dispose();
        Callback = callback.IsDisposed ? null : callback;
    }

    internal void ReleaseCallback()
    {
        Callback?.Dispose();
        Callback = null;
    }

    internal void Refresh(CefSharp.DownloadItem source, string? targetFilePath = null)
    {
        _source = source;
        if (!string.IsNullOrWhiteSpace(targetFilePath)) TargetFilePath = targetFilePath;

        var selectedPath = !string.IsNullOrWhiteSpace(TargetFilePath) ? TargetFilePath : source.FullPath;
        var suggestedName = source.SuggestedFileName;
        FileName = Path.GetFileName(selectedPath ?? string.Empty);
        if (string.IsNullOrWhiteSpace(FileName)) FileName = Path.GetFileName(suggestedName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(FileName)) FileName = "download";
    }

    private string SizeText => TotalBytes > 0
        ? $"{Format(ReceivedBytes)} de {Format(TotalBytes)}"
        : $"{Format(ReceivedBytes)} recebidos";

    private static string Format(long bytes)
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

        return unit == 0 ? $"{bytes} {units[unit]}" : $"{value:0.#} {units[unit]}";
    }
}

/// <summary>Recebe downloads do Chromium e conserva o painel próprio do Rumo.</summary>
internal sealed class DownloadManager : IDisposable
{
    private const int MaxItems = 100;

    private readonly List<DownloadItem> _items = [];
    private readonly Dictionary<int, DownloadItem> _byId = [];
    private readonly SettingsStore _settings;
    private readonly Form _owner;
    private readonly System.Windows.Forms.Timer _cleanupTimer;
    private readonly CefDownloadHandler _handler;

    public DownloadManager(SettingsStore settings, Form owner)
    {
        _settings = settings;
        _owner = owner;
        _handler = new CefDownloadHandler(this);

        _cleanupTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
        _cleanupTimer.Tick += (_, _) =>
        {
            if (RemoveStale()) NotifyChanged();
        };
        _cleanupTimer.Start();
    }

    public event EventHandler? RefreshRequested;

    public IReadOnlyList<DownloadItem> Items => _items;

    public bool HasRunningDownloads => _items.Any(item => item.IsRunning);

    internal IDownloadHandler Handler => _handler;

    public void Cancel(DownloadItem item)
    {
        try
        {
            if (item.IsRunning && item.Callback is { IsDisposed: false } callback) callback.Cancel();
        }
        catch (Exception exception) when (exception is InvalidOperationException)
        {
            // O Chromium pode concluir ou descartar a transferência antes do clique.
        }

        NotifyChanged();
    }

    public void RemoveCompleted(DownloadItem item)
    {
        if (item.IsRunning) return;
        Remove(item);
        NotifyChanged();
    }

    public void ClearCompleted()
    {
        foreach (var item in _items.Where(item => !item.IsRunning).ToList()) Remove(item);
        NotifyChanged();
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
        foreach (var item in _items) item.ReleaseCallback();
    }

    internal bool Begin(CefSharp.DownloadItem source, IBeforeDownloadCallback callback)
    {
        try
        {
            var askWhere = _settings.Current.AskWhereToSaveDownloads;
            var path = askWhere ? string.Empty : ChooseAutomaticPath(source);
            callback.Continue(path, askWhere);
            ScheduleUpdate(source, path, null);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or System.Reflection.TargetInvocationException)
        {
            // Se a pasta configurada falhar, o diálogo nativo ainda permite salvar o arquivo.
            try { callback.Continue(string.Empty, showDialog: true); }
            catch (Exception callbackException) when (callbackException is InvalidOperationException) { }
            ShowDownloadError(exception);
        }
        finally
        {
            callback.Dispose();
        }

        return true;
    }

    internal void Update(CefSharp.DownloadItem source, IDownloadItemCallback callback) =>
        ScheduleUpdate(source, null, callback);

    private void ScheduleUpdate(CefSharp.DownloadItem source, string? targetPath, IDownloadItemCallback? callback)
    {
        if (_owner.IsDisposed || !_owner.IsHandleCreated)
        {
            callback?.Dispose();
            return;
        }

        void Apply()
        {
            if (_owner.IsDisposed) return;

            if (!_byId.TryGetValue(source.Id, out var item))
            {
                item = new DownloadItem(source, targetPath);
                _items.Insert(0, item);
                _byId[source.Id] = item;
                Trim();
            }
            else
            {
                item.Refresh(source, targetPath);
            }

            if (callback is not null) item.ReplaceCallback(callback);
            if (!item.IsRunning) item.ReleaseCallback();
            if (item.IsStale) Remove(item);
            NotifyChanged();
        }

        try
        {
            if (_owner.InvokeRequired) _owner.BeginInvoke((Action)Apply);
            else Apply();
        }
        catch (Exception exception) when (exception is InvalidOperationException)
        {
            callback?.Dispose();
            // A janela pode fechar enquanto o CEF entrega atualizações finais.
        }
    }

    private string ChooseAutomaticPath(CefSharp.DownloadItem source)
    {
        var folder = _settings.ResolveDownloadFolder();
        var fileName = SafeFileName(source.SuggestedFileName);
        Directory.CreateDirectory(folder);
        return UniqueFilePath(folder, fileName);
    }

    private static string SafeFileName(string? name)
    {
        var candidate = Path.GetFileName(name ?? string.Empty);
        if (string.IsNullOrWhiteSpace(candidate) || candidate.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || candidate is "." or "..")
            return "download";
        return candidate;
    }

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

    private void ShowDownloadError(Exception exception)
    {
        if (_owner.IsDisposed || !_owner.IsHandleCreated) return;
        try
        {
            if (_owner.InvokeRequired)
            {
                _owner.BeginInvoke(new Action(() => ShowDownloadError(exception)));
                return;
            }

            MessageBox.Show(
                _owner,
                "Não foi possível iniciar o download. Verifique a pasta em Menu › Configurações.\n\n" + exception.Message,
                "Downloads",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception invokeException) when (invokeException is InvalidOperationException)
        {
        }
    }

    private void NotifyChanged() => RefreshRequested?.Invoke(this, EventArgs.Empty);

    private bool RemoveStale()
    {
        var stale = _items.Where(item => item.IsStale).ToList();
        foreach (var item in stale) Remove(item);
        return stale.Count > 0;
    }

    private void Remove(DownloadItem item)
    {
        item.ReleaseCallback();
        _items.Remove(item);
        _byId.Remove(item.Id);
    }

    private void Trim()
    {
        for (var index = _items.Count - 1; index >= 0 && _items.Count > MaxItems; index--)
        {
            if (!_items[index].IsRunning) Remove(_items[index]);
        }
    }

    private sealed class CefDownloadHandler(DownloadManager manager) : DownloadHandler
    {
        protected override bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser,
            CefSharp.DownloadItem downloadItem, IBeforeDownloadCallback callback) =>
            manager.Begin(downloadItem, callback);

        protected override void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser,
            CefSharp.DownloadItem downloadItem, IDownloadItemCallback callback) =>
            manager.Update(downloadItem, callback);
    }
}

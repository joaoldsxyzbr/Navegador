using System.Diagnostics;
using Navegador.Core.Storage;
using Navegador.Windows.Ui;

namespace Navegador.Windows;

/// <summary>Lista de downloads com abrir, abrir pasta, cancelar e limpar.</summary>
internal sealed class DownloadsForm : BrowserListForm
{
    private readonly DownloadManager _manager;
    private readonly SettingsStore _settings;
    private readonly Button _openButton;
    private readonly Button _folderButton;
    private readonly Button _cancelButton;
    private readonly Button _clearButton;

    public DownloadsForm(DownloadManager manager, SettingsStore settings)
        : base($"Downloads do {Branding.Name}", new Size(780, 460))
    {
        _manager = manager;
        _settings = settings;

        _openButton = AddButton("Abrir arquivo", (_, _) => OpenSelected(), primary: true);
        _folderButton = AddButton("Abrir pasta", (_, _) => OpenFolder());
        _cancelButton = AddButton("Cancelar", (_, _) => CancelSelected());
        _clearButton = AddButton("Limpar concluídos", (_, _) => ClearCompleted());

        Reload();
    }

    protected override void ConfigureColumns()
    {
        List.Columns.Add("Arquivo", 240);
        List.Columns.Add("Origem", 170);
        List.Columns.Add("Situação", 220);
        List.Columns.Add("Caminho", 260);
    }

    protected override IEnumerable<ListViewItem> BuildRows()
    {
        var query = SearchText;

        foreach (var item in _manager.Items)
        {
            if (query.Length > 0 &&
                !item.FileName.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                !item.DisplayHost.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var row = new ListViewItem(item.FileName) { Name = item.FileName + item.StartedAt.Ticks };
            row.SubItems.Add(item.DisplayHost);
            row.SubItems.Add(item.StatusText);
            row.SubItems.Add(item.ResultFilePath ?? string.Empty);
            row.Tag = item;
            yield return row;
        }
    }

    protected override void OnSelectionChanged()
    {
        var item = SelectedItem();
        _openButton.Enabled = item is { IsComplete: true };
        _cancelButton.Enabled = item is { IsRunning: true };
    }

    protected override void ActivateSelected() => OpenSelected();

    protected override void OnReloaded()
    {
        _clearButton.Enabled = _manager.Items.Any(item => !item.IsRunning);
        Footer.Text = $"Pasta de destino: {_settings.ResolveDownloadFolder()}";
    }

    private DownloadItem? SelectedItem() => SelectedRow?.Tag as DownloadItem;

    private void OpenSelected()
    {
        if (SelectedItem() is not { IsComplete: true } item) return;

        Open(item.ResultFilePath);
    }

    private void OpenFolder()
    {
        var selected = SelectedItem();
        var folder = selected?.ResultFilePath is { } path && File.Exists(path)
            ? Path.GetDirectoryName(path)
            : _settings.ResolveDownloadFolder();

        Open(folder);
    }

    private void CancelSelected()
    {
        if (SelectedItem() is not { } item) return;

        _manager.Cancel(item);
        Reload();
    }

    private void ClearCompleted()
    {
        _manager.ClearCompleted();
        Reload();
    }

    private void Open(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            MessageBox.Show(this, $"Não foi possível abrir:\n{path}\n\n{exception.Message}", "Downloads",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

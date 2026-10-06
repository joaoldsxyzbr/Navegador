using Navegador.Core.Storage;
using Navegador.Windows.Ui;

namespace Navegador.Windows;

/// <summary>Histórico de navegação com busca.</summary>
internal sealed class HistoryForm : BrowserListForm
{
    private readonly HistoryStore _store;
    private readonly Action<string> _openUrl;
    private readonly Button _openButton;
    private readonly Button _removeButton;

    public HistoryForm(HistoryStore store, Action<string> openUrl)
        : base("Histórico do Navegador", new Size(780, 500))
    {
        _store = store;
        _openUrl = openUrl;

        _openButton = AddButton("Abrir", (_, _) => ActivateSelected(), primary: true);
        _removeButton = AddButton("Remover do histórico", (_, _) => RemoveSelected());
        AddButton("Limpar tudo", (_, _) => ClearAll());

        Reload();
    }

    protected override void ConfigureColumns()
    {
        List.Columns.Add("Título", 280);
        List.Columns.Add("Endereço", 360);
        List.Columns.Add("Última visita", 130);
    }

    protected override IEnumerable<ListViewItem> BuildRows()
    {
        foreach (var entry in _store.Search(SearchText))
        {
            var row = new ListViewItem(entry.Title) { Name = entry.Url };
            row.SubItems.Add(entry.Url);
            row.SubItems.Add(FormatWhen(entry.LastVisitedAt));
            yield return row;
        }
    }

    protected override void OnSelectionChanged()
    {
        var hasSelection = SelectedRow is not null;
        _openButton.Enabled = hasSelection;
        _removeButton.Enabled = hasSelection;
    }

    protected override void ActivateSelected()
    {
        if (SelectedRow is not { } row) return;

        _openUrl(row.Name);
        Close();
    }

    protected override void OnReloaded()
    {
        Footer.Text = _store.Count == 1 ? "1 página registrada" : $"{_store.Count} páginas registradas";
    }

    private void RemoveSelected()
    {
        if (SelectedRow is not { } row) return;

        _store.Remove(row.Name);
        Reload();
    }

    private void ClearAll()
    {
        if (_store.Count == 0) return;

        var answer = MessageBox.Show(
            this,
            "Apagar todo o histórico de navegação?",
            "Histórico",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (answer != DialogResult.Yes) return;

        _store.Clear();
        Reload();
    }

    private static string FormatWhen(DateTimeOffset moment)
    {
        var local = moment.ToLocalTime();
        var today = DateTimeOffset.Now.Date;

        if (local.Date == today) return $"hoje {local:HH:mm}";
        if (local.Date == today.AddDays(-1)) return $"ontem {local:HH:mm}";

        return local.ToString("dd/MM/yyyy HH:mm");
    }
}

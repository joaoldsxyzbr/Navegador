using Navegador.Core.Storage;
using Navegador.Windows.Ui;

namespace Navegador.Windows;

/// <summary>Gerencia os favoritos: abrir, renomear, remover e limpar.</summary>
internal sealed class FavoritesForm : BrowserListForm
{
    private readonly FavoritesStore _store;
    private readonly Action<string> _openUrl;
    private readonly Func<(string Url, string Title)?> _currentPage;
    private readonly Button _removeButton;
    private readonly Button _renameButton;
    private readonly Button _openButton;

    public FavoritesForm(FavoritesStore store, Action<string> openUrl, Func<(string Url, string Title)?> currentPage)
        : base("Favoritos do Navegador", new Size(720, 460))
    {
        _store = store;
        _openUrl = openUrl;
        _currentPage = currentPage;

        AddButton("Adicionar a página atual", (_, _) => AddCurrent());
        _openButton = AddButton("Abrir", (_, _) => ActivateSelected(), primary: true);
        _renameButton = AddButton("Renomear", (_, _) => RenameSelected());
        _removeButton = AddButton("Remover", (_, _) => RemoveSelected());

        Reload();
    }

    protected override void ConfigureColumns()
    {
        List.Columns.Add("Título", 260);
        List.Columns.Add("Endereço", 400);
    }

    protected override IEnumerable<ListViewItem> BuildRows()
    {
        foreach (var favorite in _store.Search(SearchText))
        {
            var row = new ListViewItem(favorite.Title) { Name = favorite.Url };
            row.SubItems.Add(favorite.Url);
            yield return row;
        }
    }

    protected override void OnSelectionChanged()
    {
        var hasSelection = SelectedRow is not null;
        _openButton.Enabled = hasSelection;
        _renameButton.Enabled = hasSelection;
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
        Footer.Text = _store.Count == 1 ? "1 favorito" : $"{_store.Count} favoritos";
    }

    private void AddCurrent()
    {
        var current = _currentPage();
        if (current is null)
        {
            MessageBox.Show(this, "Nenhuma página aberta para adicionar.", "Favoritos",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var added = _store.Add(current.Value.Url, current.Value.Title);
        Reload();

        if (!added)
        {
            MessageBox.Show(this, "Esta página já está nos favoritos.", "Favoritos",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void RenameSelected()
    {
        if (SelectedRow is not { } row) return;
        if (_store.Find(row.Name) is not { } favorite) return;

        var newTitle = PromptForTitle(favorite.Title);
        if (newTitle is null) return;

        favorite.Title = newTitle;
        _store.Save();
        Reload();
    }

    private void RemoveSelected()
    {
        if (SelectedRow is not { } row) return;

        _store.Remove(row.Name);
        Reload();
    }

    private string? PromptForTitle(string current)
    {
        using var dialog = new Form
        {
            Text = "Renomear favorito",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(420, 110),
            BackColor = Theme.TitleBar,
            ForeColor = Theme.Text,
            MinimizeBox = false,
            MaximizeBox = false,
            Font = Theme.Ui(10F)
        };

        var input = new TextBox
        {
            Text = current,
            Dock = DockStyle.Top,
            BackColor = Theme.Address,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle
        };

        var ok = new Button { Text = "Salvar", DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
        ok.FlatAppearance.BorderColor = Theme.Hover;
        cancel.FlatAppearance.BorderColor = Theme.Hover;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40,
            Padding = new Padding(6)
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        dialog.Controls.Add(input);
        dialog.Controls.Add(buttons);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;

        input.SelectAll();

        return dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(input.Text)
            ? input.Text.Trim()
            : null;
    }
}

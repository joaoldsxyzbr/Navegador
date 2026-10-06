using Navegador.Windows.Ui;

namespace Navegador.Windows.Ui;

/// <summary>
/// Base das janelas de lista (favoritos, histórico, downloads): tema escuro,
/// campo de busca, lista com colunas e uma fila de botões embaixo.
/// </summary>
internal abstract class BrowserListForm : Form
{
    private readonly TextBox _search = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Theme.Address,
        ForeColor = Theme.Text,
        Font = Theme.Ui(10F),
        PlaceholderText = "Pesquisar"
    };

    private readonly FlowLayoutPanel _buttonBar = new()
    {
        Dock = DockStyle.Bottom,
        Height = 46,
        FlowDirection = FlowDirection.LeftToRight,
        Padding = new Padding(8, 6, 8, 6),
        BackColor = Theme.Toolbar
    };

    private readonly Dictionary<string, ListViewItem> _rows = new(StringComparer.Ordinal);
    private bool _suppressSelectionEvents;

    protected BrowserListForm(string title, Size size)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(520, 320);
        Size = size;
        BackColor = Theme.TitleBar;
        ForeColor = Theme.Text;
        Font = Theme.Ui(10F);

        List = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Address,
            ForeColor = Theme.Text,
            Font = Theme.Ui(10F),
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        List.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressSelectionEvents) return;
            OnSelectionChanged();
        };
        List.DoubleClick += (_, _) => ActivateSelected();

        ConfigureColumns();

        var searchRow = new Panel
        {
            Dock = DockStyle.Top,
            Height = 38,
            Padding = new Padding(8, 6, 8, 4),
            BackColor = Theme.TitleBar
        };
        searchRow.Controls.Add(_search);
        _search.TextChanged += (_, _) => Reload();

        var footer = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            ForeColor = Theme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0)
        };
        Footer = footer;

        Controls.Add(List);
        Controls.Add(_buttonBar);
        Controls.Add(footer);
        Controls.Add(searchRow);
    }

    protected ListView List { get; }

    protected Label Footer { get; }

    protected string SearchText => _search.Text.Trim();

    protected abstract void ConfigureColumns();

    protected abstract IEnumerable<ListViewItem> BuildRows();

    protected abstract void OnSelectionChanged();

    protected abstract void ActivateSelected();

    protected virtual void OnReloaded()
    {
    }

    protected Button AddButton(string text, EventHandler? onClick = null, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Theme.AddressFocus : Theme.Address,
            ForeColor = Theme.Text,
            Font = Theme.Ui(9.5F),
            Padding = new Padding(6, 0, 6, 0),
            Margin = new Padding(0, 0, 6, 0),
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderColor = Theme.Hover;
        if (onClick is not null) button.Click += onClick;

        _buttonBar.Controls.Add(button);
        return button;
    }

    /// <summary>Redesenha a lista preservando a seleção e a rolagem.</summary>
    protected void Reload()
    {
        var selectedKey = SelectedKey();
        var scroll = List.TopItem?.Index ?? 0;

        _suppressSelectionEvents = true;

        try
        {
            var rows = BuildRows().ToList();
            _rows.Clear();

            List.BeginUpdate();
            List.Items.Clear();

            foreach (var row in rows)
            {
                _rows[row.Name] = row;
                List.Items.Add(row);
            }

            if (List.Columns.Count > 0)
            {
                // A última coluna ocupa o espaço restante.
                var used = List.Columns.Cast<ColumnHeader>().Take(List.Columns.Count - 1).Sum(column => column.Width);
                List.Columns[^1].Width = Math.Max(120, List.ClientSize.Width - used - 4);
            }

            List.EndUpdate();

            if (selectedKey is not null && _rows.TryGetValue(selectedKey, out var restored))
            {
                restored.Selected = true;
                restored.EnsureVisible();
            }
            else if (List.Items.Count > 0 && scroll < List.Items.Count)
            {
                List.EnsureVisible(scroll);
            }
        }
        finally
        {
            _suppressSelectionEvents = false;
        }

        OnSelectionChanged();
        OnReloaded();
    }

    protected ListViewItem? SelectedRow =>
        List.SelectedItems.Count > 0 ? List.SelectedItems[0] : null;

    private string? SelectedKey() => SelectedRow?.Name;
}

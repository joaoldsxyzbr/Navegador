using Navegador.Windows.Ui;

namespace Navegador.Windows.Ui;

/// <summary>
/// Base das janelas de favoritos, histórico e downloads.
/// Mantém busca, lista, estados vazios e ações com o mesmo padrão visual.
/// </summary>
internal abstract class BrowserListForm : Form
{
    private readonly TextBox _search = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Address,
        ForeColor = Theme.Text,
        Font = Theme.Ui(10F),
        PlaceholderText = "Pesquisar por nome ou endereço",
        AccessibleName = "Pesquisar nesta lista",
        AutoSize = false,
        Margin = Padding.Empty
    };

    private readonly FlowLayoutPanel _buttonBar = new()
    {
        Dock = DockStyle.Fill,
        Height = 50,
        FlowDirection = FlowDirection.LeftToRight,
        Padding = new Padding(0, 7, 0, 0),
        BackColor = Theme.TitleBar,
        WrapContents = false
    };

    private readonly Label _emptyState = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Theme.MutedText,
        BackColor = Theme.Address,
        Font = Theme.Ui(11F),
        TextAlign = ContentAlignment.MiddleCenter,
        AccessibleRole = AccessibleRole.StaticText
    };

    private readonly Dictionary<string, ListViewItem> _rows = new(StringComparer.Ordinal);
    private bool _suppressSelectionEvents;

    protected BrowserListForm(string title, Size size)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(680, 400);
        Size = size;
        BackColor = Theme.TitleBar;
        ForeColor = Theme.Text;
        Font = Theme.Ui(10F);
        AutoScaleMode = AutoScaleMode.Font;

        var titleLabel = new Label
        {
            Text = title.Replace(" do " + Branding.Name, string.Empty, StringComparison.Ordinal),
            Dock = DockStyle.Top,
            Height = 31,
            Font = Theme.Ui(16F),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var subtitle = new Label
        {
            Text = "Pesquise pelo nome ou endereço. Dê dois cliques para abrir.",
            Dock = DockStyle.Bottom,
            Height = 27,
            Font = Theme.Ui(9F),
            ForeColor = Theme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.TitleBar,
            Padding = new Padding(0, 2, 0, 0)
        };
        header.Controls.Add(titleLabel);
        header.Controls.Add(subtitle);

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
            Font = Theme.Ui(9.5F),
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            AccessibleName = title,
            OwnerDraw = true,
            UseCompatibleStateImageBehavior = false
        };
        List.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressSelectionEvents) return;
            OnSelectionChanged();
        };
        List.DoubleClick += (_, _) => ActivateSelected();
        List.DrawColumnHeader += DrawColumnHeader;
        List.DrawItem += DrawListItem;
        List.DrawSubItem += DrawListSubItem;
        List.Resize += (_, _) => FitLastColumn();

        ConfigureColumns();

        var searchShell = new RoundedPanel(10)
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Address,
            BorderColor = Theme.AddressBorder,
            BorderWidth = 1F,
            Padding = new Padding(14, 7, 14, 6),
            Margin = new Padding(0, 3, 0, 5)
        };
        searchShell.Controls.Add(_search);
        _search.TextChanged += (_, _) => Reload();

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Address,
            Margin = new Padding(0, 5, 0, 5)
        };
        body.Controls.Add(List);
        body.Controls.Add(_emptyState);
        _emptyState.BringToFront();

        var footer = new Label
        {
            Dock = DockStyle.Fill,
            Height = 28,
            ForeColor = Theme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Padding = new Padding(2, 0, 0, 0)
        };
        Footer = footer;

        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(20, 16, 20, 12),
            BackColor = Theme.TitleBar,
            Margin = Padding.Empty
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        page.Controls.Add(header, 0, 0);
        page.Controls.Add(searchShell, 0, 1);
        page.Controls.Add(body, 0, 2);
        page.Controls.Add(Footer, 0, 3);
        page.Controls.Add(_buttonBar, 0, 4);
        Controls.Add(page);
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
            Height = 36,
            MinimumSize = new Size(0, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Theme.AddressFocus : Theme.ActiveTab,
            ForeColor = Theme.Text,
            Font = Theme.Ui(9.5F),
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(0, 0, 8, 0),
            UseVisualStyleBackColor = false,
            AccessibleName = text,
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderColor = Theme.AddressBorder;
        button.FlatAppearance.MouseOverBackColor = Theme.Hover;
        if (onClick is not null) button.Click += onClick;

        _buttonBar.Controls.Add(button);
        return button;
    }

    /// <summary>Redesenha a lista preservando a seleção e a posição de rolagem.</summary>
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

            FitLastColumn();

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
            List.EndUpdate();
            _suppressSelectionEvents = false;
        }

        var isEmpty = List.Items.Count == 0;
        List.Visible = !isEmpty;
        _emptyState.Visible = isEmpty;
        _emptyState.Text = SearchText.Length > 0
            ? $"Nenhum resultado para “{SearchText}”."
            : "Ainda não há itens nesta lista.";

        OnSelectionChanged();
        OnReloaded();
    }

    protected ListViewItem? SelectedRow =>
        List.SelectedItems.Count > 0 ? List.SelectedItems[0] : null;

    private string? SelectedKey() => SelectedRow?.Name;

    private void DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs eventArgs)
    {
        using var background = new SolidBrush(Theme.ActiveTab);
        eventArgs.Graphics.FillRectangle(background, eventArgs.Bounds);

        var textBounds = Rectangle.Inflate(eventArgs.Bounds, -10, 0);
        TextRenderer.DrawText(
            eventArgs.Graphics,
            eventArgs.Header?.Text ?? string.Empty,
            List.Font,
            textBounds,
            Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        using var divider = new Pen(Theme.AddressBorder);
        eventArgs.Graphics.DrawLine(
            divider,
            eventArgs.Bounds.Left,
            eventArgs.Bounds.Bottom - 1,
            eventArgs.Bounds.Right,
            eventArgs.Bounds.Bottom - 1);
    }

    private void DrawListItem(object? sender, DrawListViewItemEventArgs eventArgs)
    {
        if (List.View != View.Details) eventArgs.DrawDefault = true;
    }

    private void DrawListSubItem(object? sender, DrawListViewSubItemEventArgs eventArgs)
    {
        var selected = eventArgs.Item?.Selected == true;
        using var background = new SolidBrush(selected ? Theme.AddressFocus : Theme.Address);
        eventArgs.Graphics.FillRectangle(background, eventArgs.Bounds);

        var textBounds = Rectangle.Inflate(eventArgs.Bounds, -10, 0);
        TextRenderer.DrawText(
            eventArgs.Graphics,
            eventArgs.SubItem?.Text ?? string.Empty,
            List.Font,
            textBounds,
            Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private void FitLastColumn()
    {
        if (List.Columns.Count == 0) return;

        var used = List.Columns.Cast<ColumnHeader>()
            .Take(List.Columns.Count - 1)
            .Sum(column => column.Width);
        var width = Math.Max(1, List.ClientSize.Width - used - 8);
        if (List.Columns[^1].Width != width) List.Columns[^1].Width = width;
    }
}

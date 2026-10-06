using Navegador.Windows.Ui;

namespace Navegador.Windows;

/// <summary>
/// Faixa de downloads na parte de baixo da janela, no estilo do Chrome.
/// Aparece quando há transferências e pode ser escondida no "×".
/// </summary>
internal sealed class DownloadsBar : Panel
{
    private readonly DownloadManager _manager;
    private readonly FlowLayoutPanel _items;
    private readonly Label _title;
    private readonly List<DownloadItem> _visible = [];
    private bool _rebuilding;

    public DownloadsBar(DownloadManager manager)
    {
        _manager = manager;

        Dock = DockStyle.Fill;
        BackColor = Theme.Toolbar;
        Height = 120;
        Padding = new Padding(10, 6, 10, 6);

        _title = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Theme.Text,
            Font = Theme.Ui(10F),
            Text = "Downloads"
        };

        var close = new Button
        {
            Text = "×",
            Dock = DockStyle.Right,
            Width = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Toolbar,
            ForeColor = Theme.Text,
            Font = Theme.Icon(12F),
            TabStop = false
        };
        close.FlatAppearance.BorderSize = 0;
        close.FlatAppearance.MouseOverBackColor = Theme.Hover;
        close.Click += (_, _) => HideBar();

        var openManager = new Button
        {
            Text = "Ver todos",
            Dock = DockStyle.Right,
            Width = 92,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Toolbar,
            ForeColor = Theme.Text,
            Font = Theme.Ui(9F),
            TabStop = false
        };
        openManager.FlatAppearance.BorderSize = 0;
        openManager.FlatAppearance.MouseOverBackColor = Theme.Hover;
        openManager.Click += (_, _) => ShowAllRequested?.Invoke(this, EventArgs.Empty);

        var header = new Panel { Dock = DockStyle.Top, Height = 26 };
        header.Controls.Add(_title);
        header.Controls.Add(openManager);
        header.Controls.Add(close);

        _items = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Theme.Toolbar,
            Padding = new Padding(0, 4, 0, 0)
        };

        Controls.Add(_items);
        Controls.Add(header);

        manager.RefreshRequested += (_, _) => RefreshItems();
        RefreshItems();
    }

    public event EventHandler? ShowAllRequested;

    private void HideBar()
    {
        Visible = false;
    }

    /// <summary>Mostra a faixa de novo, por exemplo ao clicar em Downloads na barra.</summary>
    public void Reveal()
    {
        Visible = true;
        RefreshItems();
    }

    /// <summary>Atualiza os cartões na tela. Não recria o que já está desenhado.</summary>
    public void RefreshItems()
    {
        if (_rebuilding || IsDisposed) return;

        _rebuilding = true;

        try
        {
            var items = _manager.Items.Take(6).ToList();

            if (items.Count == 0)
            {
                Visible = false;
                Clear();
                return;
            }

            _title.Text = _manager.HasRunningDownloads
                ? "Downloads em andamento"
                : "Downloads";

            if (!Visible && _manager.HasRunningDownloads) Visible = true;

            // Estrutura mudou: reconstrói.
            if (!items.SequenceEqual(_visible))
            {
                Clear();

                foreach (var item in items)
                {
                    var card = new DownloadCard(item, _manager);
                    _items.Controls.Add(card);
                    _visible.Add(item);
                }

                return;
            }

            // Só o progresso mudou: atualiza no lugar.
            for (var index = 0; index < _items.Controls.Count && index < items.Count; index++)
            {
                if (_items.Controls[index] is DownloadCard card) card.Update(items[index]);
            }
        }
        finally
        {
            _rebuilding = false;
        }
    }

    private void Clear()
    {
        foreach (Control control in _items.Controls) control.Dispose();

        _items.Controls.Clear();
        _visible.Clear();
    }

    /// <summary>Cartão de um download: nome, barra de progresso e ações.</summary>
    private sealed class DownloadCard : Panel
    {
        private readonly Label _name;
        private readonly Label _status;
        private readonly ProgressBar _progress;
        private readonly Button _action;
        private DownloadItem _item;

        public DownloadCard(DownloadItem item, DownloadManager manager)
        {
            _item = item;

            Width = 420;
            Height = 54;
            Margin = new Padding(0, 0, 8, 4);
            BackColor = Theme.TitleBar;
            Padding = new Padding(8, 4, 8, 4);

            _name = new Label
            {
                Dock = DockStyle.Top,
                Height = 18,
                ForeColor = Theme.Text,
                Font = Theme.Ui(9.5F),
                AutoEllipsis = true,
                Text = item.FileName
            };

            _status = new Label
            {
                Dock = DockStyle.Top,
                Height = 16,
                ForeColor = Theme.MutedText,
                Font = Theme.Ui(8.5F),
                AutoEllipsis = true
            };

            _progress = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 6,
                Style = ProgressBarStyle.Continuous,
                Maximum = 100
            };

            _action = new Button
            {
                Dock = DockStyle.Right,
                Width = 74,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Address,
                ForeColor = Theme.Text,
                Font = Theme.Ui(8.5F),
                TabStop = false
            };
            _action.FlatAppearance.BorderColor = Theme.Hover;
            _action.Click += (_, _) => HandleAction(manager);

            Controls.Add(_progress);
            Controls.Add(_action);
            Controls.Add(_status);
            Controls.Add(_name);

            Update(item);
        }

        public void Update(DownloadItem item)
        {
            _item = item;

            _name.Text = item.FileName;
            _status.Text = $"{item.DisplayHost} — {item.StatusText}";
            _progress.Value = Math.Clamp(item.Progress, 0, 100);

            _action.Text = item.IsRunning ? "Cancelar" : item.IsInterrupted ? "Fechar" : "Abrir";
        }

        private void HandleAction(DownloadManager manager)
        {
            if (_item.IsRunning)
            {
                manager.Cancel(_item);
                return;
            }

            manager.RemoveCompleted(_item);
        }
    }
}

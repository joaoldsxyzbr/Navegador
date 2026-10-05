using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Navegador.Services;

namespace Navegador;

internal sealed class BrowserSettingsPage : ContentPage
{
    private readonly BrowserUpdateService _updateService = new();
    private readonly Label _statusLabel = new();
    private readonly Label _notesLabel = new() { IsVisible = false };
    private readonly Button _checkButton = new() { Text = "Verificar atualizações" };
    private readonly Button _installButton = new()
    {
        Text = "Atualizar agora",
        IsVisible = false,
        IsEnabled = false
    };
    private UpdateCheckResult? _availableUpdate;

    public BrowserSettingsPage()
    {
        Title = "Configurações";
        _statusLabel.Text = $"Versão instalada: {AppInfo.Current.VersionString}";
        _statusLabel.FontSize = 16;

        _checkButton.Clicked += OnCheckUpdatesClicked;
        _installButton.Clicked += OnInstallUpdateClicked;

        var closeButton = new Button { Text = "Fechar" };
        closeButton.Clicked += async (_, _) => await Navigation.PopModalAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 16,
                Children =
                {
                    new Label
                    {
                        Text = "Sobre e atualizações",
                        FontSize = 22,
                        FontAttributes = FontAttributes.Bold
                    },
                    _statusLabel,
                    _checkButton,
                    _notesLabel,
                    _installButton,
                    closeButton
                }
            }
        };
    }

    private async void OnCheckUpdatesClicked(object? sender, EventArgs e)
    {
        _checkButton.IsEnabled = false;
        _installButton.IsVisible = false;
        _installButton.IsEnabled = false;
        _notesLabel.IsVisible = false;
        _availableUpdate = null;
        _statusLabel.Text = "Verificando atualizações…";

        try
        {
            var result = await _updateService.CheckLatestAsync();
            _availableUpdate = result;

            switch (result.Status)
            {
                case UpdateStatus.NoRelease:
                    _statusLabel.Text = "Ainda não há uma versão publicada.";
                    break;
                case UpdateStatus.UpToDate:
                    _statusLabel.Text = "Você está na versão mais recente.";
                    break;
                case UpdateStatus.MissingPackage:
                    _statusLabel.Text = $"A versão {result.Version} foi publicada, mas ainda não tem pacote para este dispositivo.";
                    ShowReleaseNotes(result.ReleaseNotes);
                    break;
                case UpdateStatus.MissingDigest:
                    _statusLabel.Text = "A release não oferece um digest SHA-256; a instalação foi bloqueada.";
                    ShowReleaseNotes(result.ReleaseNotes);
                    break;
                case UpdateStatus.Available:
                    _statusLabel.Text = $"Nova versão disponível: {result.Version}";
                    _installButton.IsVisible = true;
                    _installButton.IsEnabled = true;
                    ShowReleaseNotes(result.ReleaseNotes);
                    break;
            }
        }
        catch (Exception)
        {
            _statusLabel.Text = "Não foi possível verificar atualizações. Confira sua conexão e tente novamente.";
        }
        finally
        {
            _checkButton.IsEnabled = true;
        }
    }

    private async void OnInstallUpdateClicked(object? sender, EventArgs e)
    {
        var update = _availableUpdate;

        if (update is null || !update.HasVerifiedPackage)
            return;

        var installationStep = DeviceInfo.Platform == DevicePlatform.Android
            ? "O Android pedirá uma confirmação final para instalar."
            : "O Navegador será fechado e abrirá novamente após a instalação.";
        var confirmed = await DisplayAlertAsync(
            "Atualizar navegador",
            $"Deseja baixar a versão {update.Version} e validar o pacote? {installationStep}",
            "Continuar",
            "Cancelar");

        if (!confirmed)
            return;

        _installButton.IsEnabled = false;
        _statusLabel.Text = "Baixando e validando o pacote…";

        try
        {
            var packagePath = await _updateService.DownloadAndVerifyAsync(update);
            var installed = false;

#if WINDOWS
            installed = await Platforms.Windows.UpdateInstaller.InstallAsync(packagePath);
#elif ANDROID
            installed = await Platforms.Android.UpdateInstaller.InstallAsync(packagePath);
#endif

            _statusLabel.Text = installed
                ? "Instalador aberto. Siga a confirmação do sistema para concluir."
                : "O pacote foi baixado e validado. Permita a instalação e toque em Atualizar agora novamente.";
        }
        catch (Exception)
        {
            _statusLabel.Text = "Não foi possível baixar ou iniciar a atualização. O navegador continua aberto.";
            _installButton.IsEnabled = true;
        }
    }

    private void ShowReleaseNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return;

        _notesLabel.Text = $"Notas da versão:\n{notes.Trim()}";
        _notesLabel.IsVisible = true;
    }
}

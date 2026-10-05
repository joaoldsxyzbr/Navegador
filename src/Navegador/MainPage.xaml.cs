using Navegador.Core;

namespace Navegador;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        Navigate(AddressResolver.HomeUrl);
    }

    private void OnGoClicked(object? sender, EventArgs e) => Navigate(AddressEntry.Text);

    private void OnAddressCompleted(object? sender, EventArgs e) => Navigate(AddressEntry.Text);

    private void OnBackClicked(object? sender, EventArgs e)
    {
        if (Browser.CanGoBack)
            Browser.GoBack();
    }

    private void OnForwardClicked(object? sender, EventArgs e)
    {
        if (Browser.CanGoForward)
            Browser.GoForward();
    }

    private void OnReloadClicked(object? sender, EventArgs e) => Browser.Reload();

    private void OnHomeClicked(object? sender, EventArgs e) => Navigate(AddressResolver.HomeUrl);

    private void OnBrowserNavigated(object? sender, WebNavigatedEventArgs e)
    {
        AddressEntry.Text = e.Url;
        BackButton.IsEnabled = Browser.CanGoBack;
        ForwardButton.IsEnabled = Browser.CanGoForward;
    }

    private void Navigate(string? input)
    {
        Browser.Source = AddressResolver.Resolve(input);
    }
}

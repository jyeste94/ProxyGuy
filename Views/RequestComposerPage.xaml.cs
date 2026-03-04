using ProxyGuy.ViewModels;

namespace ProxyGuy.Views;

public partial class RequestComposerPage : ContentPage
{
    public RequestComposerPage(RequestComposerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}

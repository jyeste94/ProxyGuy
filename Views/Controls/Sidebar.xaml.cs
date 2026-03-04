namespace ProxyGuy.Views.Controls;

public partial class Sidebar : ContentView
{
    public Sidebar()
    {
        InitializeComponent();
    }

    public void FocusFilter()
    {
        FilterEntry?.Focus();
    }
}

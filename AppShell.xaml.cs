namespace ProxyGuy
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("CertificatePage", typeof(Views.CertificatePage));
        }
    }
}

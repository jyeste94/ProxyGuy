using System;

namespace ProxyGuyMAUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            try
            {
                MainPage = new AppShell();
            }
            catch (Exception ex)
            {
                CrashLogger.Log("App constructor", ex);
                throw;
            }
        }
    }
}

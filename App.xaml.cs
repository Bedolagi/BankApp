using System.Windows;

namespace BankApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {

            Database.InitializeDatabase();

            var mainWindow = new MainWindow();
            mainWindow.MainFrame.Navigate(new AuthPage());
            mainWindow.Show();

            base.OnStartup(e);
        }
    }
}
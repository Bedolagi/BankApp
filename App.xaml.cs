using System.Windows;

namespace BankApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            
            Database.InitializeDatabase();

            
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
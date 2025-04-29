using Npgsql;
using System.Windows;
using System.Windows.Controls;

namespace BankApp
{
    public partial class AuthPage : Page
    {
        public AuthPage()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;

            using (var conn = new NpgsqlConnection(Database.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT id FROM users WHERE login = @login AND password = @password", conn))
                {
                    cmd.Parameters.AddWithValue("login", login);
                    cmd.Parameters.AddWithValue("password", password);

                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        int userId = (int)result;
                    }
                    else
                    {
                        MessageBox.Show("Неправильный логин или пароль!");
                    }
                }
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;

            using (var conn = new NpgsqlConnection(Database.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("INSERT INTO users (login, password) VALUES (@login, @password) RETURNING id", conn))
                {
                    cmd.Parameters.AddWithValue("login", login);
                    cmd.Parameters.AddWithValue("password", password);

                    int userId = (int)cmd.ExecuteScalar();
                    MessageBox.Show("Успешная регистрация.");
                }
            }
        }
    }
}
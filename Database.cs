using Npgsql;

namespace BankApp
{
    public static class Database
    {
        public static string ConnectionString = "Host=localhost;Username=admin1234;Password=0000;Database=BankApp";

        public static void InitializeDatabase()
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(
                    "CREATE TABLE IF NOT EXISTS users (" +
                    "id SERIAL PRIMARY KEY, " +
                    "login TEXT NOT NULL UNIQUE, " +
                    "password TEXT NOT NULL)", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new NpgsqlCommand(
                    "CREATE TABLE IF NOT EXISTS loans (" +
                    "id SERIAL PRIMARY KEY, " +
                    "user_id INTEGER REFERENCES users(id), " +
                    "amount DECIMAL NOT NULL, " +
                    "term INTEGER NOT NULL, " +
                    "rate DECIMAL NOT NULL, " +
                    "date TIMESTAMP NOT NULL)", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
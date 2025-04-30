using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Windows.Input;

namespace BankApp
{
    public partial class AccountPage : System.Windows.Controls.Page
    {
        private int _userId;

        public AccountPage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadUserData();
            LoadLoans();
        }

        private void LoadUserData()
        {
            using (var conn = new NpgsqlConnection(Database.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT login FROM users WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("id", _userId);
                    string login = (string)cmd.ExecuteScalar();
                    WelcomeText.Text = $"Здравствуйте, {login}!";
                }
            }
        }

        private void LoadLoans()
        {
            List<Loan> loans = new List<Loan>();

            using (var conn = new NpgsqlConnection(Database.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT amount, term, rate, date FROM loans WHERE user_id = @user_id", conn))
                {
                    cmd.Parameters.AddWithValue("user_id", _userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            loans.Add(new Loan
                            {
                                Amount = reader.GetDecimal(0),
                                Term = reader.GetInt32(1),
                                Rate = reader.GetDecimal(2),
                                Date = reader.GetDateTime(3)
                            });
                        }
                    }
                }
            }

            LoansListView.ItemsSource = loans;
        }

        private void TakeLoanButton_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(AmountTextBox.Text, out decimal amount) ||
                !int.TryParse(TermTextBox.Text, out int term) ||
                !decimal.TryParse(RateTextBox.Text, out decimal rate))
            {
                MessageBox.Show("В полях указывайте только числа!");
                return;
            }

            using (var conn = new NpgsqlConnection(Database.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("INSERT INTO loans (user_id, amount, term, rate, date) VALUES (@user_id, @amount, @term, @rate, @date)", conn))
                {
                    cmd.Parameters.AddWithValue("user_id", _userId);
                    cmd.Parameters.AddWithValue("amount", amount);
                    cmd.Parameters.AddWithValue("term", term);
                    cmd.Parameters.AddWithValue("rate", rate);
                    cmd.Parameters.AddWithValue("date", DateTime.Now);

                    cmd.ExecuteNonQuery();
                }
            }

            LoadLoans();
            MessageBox.Show("Кредит оформлен успешно.");
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoansListView.SelectedItem is Loan selectedLoan)
            {
                var paymentSchedule = CalculatePaymentSchedule(selectedLoan);
                ExportToExcel(paymentSchedule, selectedLoan);
                MessageBox.Show("График платежей успешно загружен.");
            }
            else
            {
                MessageBox.Show("Выберите кредит");
            }
        }
        private void LoansListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LoansListView.SelectedItem is Loan selectedLoan)
            {
                var paymentSchedule = CalculatePaymentSchedule(selectedLoan);
                var window = new PaymentScheduleWindow(paymentSchedule);
                window.Show();
            }
        }
        private List<Payment> CalculatePaymentSchedule(Loan loan)
        {
            var payments = new List<Payment>();
            decimal monthlyRate = loan.Rate / 100 / 12;
            decimal monthlyPayment = loan.Amount * monthlyRate * (decimal)Math.Pow(1 + (double)monthlyRate, loan.Term) /
                                   (decimal)(Math.Pow(1 + (double)monthlyRate, loan.Term) - 1);

            decimal remainingBalance = loan.Amount;
            DateTime paymentDate = DateTime.Now;

            for (int i = 1; i <= loan.Term; i++)
            {
                paymentDate = paymentDate.AddMonths(1);
                decimal interest = remainingBalance * monthlyRate;
                decimal principal = monthlyPayment - interest;
                remainingBalance -= principal;

                payments.Add(new Payment
                {
                    Number = i,
                    Date = paymentDate,
                    PaymentAmount = monthlyPayment,
                    Principal = principal,
                    Interest = interest,
                    RemainingBalance = remainingBalance > 0 ? remainingBalance : 0
                });
            }

            return payments;
        }

        private void ExportToExcel(List<Payment> payments, Loan loan)
        {
            string fileName = $"График_платежей_{loan.Amount}_{loan.Term}месяцев.xlsx";

            using (SpreadsheetDocument document = SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet()
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "График платежей"
                };
                sheets.Append(sheet);

                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                Row headerRow = new Row();
                headerRow.Append(
                    CreateCell("№ платежа"),
                    CreateCell("Дата платежа"),
                    CreateCell("Сумма платежа"),
                    CreateCell("Основной долг"),
                    CreateCell("Проценты"),
                    CreateCell("Остаток долга"));
                sheetData.Append(headerRow);

               
                foreach (var payment in payments)
                {
                    Row dataRow = new Row();
                    dataRow.Append(
                        CreateCell(payment.Number.ToString(), CellValues.Number),
                        CreateCell(payment.Date.ToShortDateString()),
                        CreateCell(payment.PaymentAmount.ToString("0.00"), CellValues.Number),
                        CreateCell(payment.Principal.ToString("0.00"), CellValues.Number),
                        CreateCell(payment.Interest.ToString("0.00"), CellValues.Number),
                        CreateCell(payment.RemainingBalance.ToString("0.00"), CellValues.Number));

                    sheetData.Append(dataRow);
                }

                workbookPart.Workbook.Save();
            }

        
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fileName)
            {
                UseShellExecute = true
            });
        }


        private Cell CreateCell(string text)
        {
            return new Cell()
            {
                CellValue = new CellValue(text),
                DataType = CellValues.String
            };
        }

        private Cell CreateCell(string text, CellValues type)
        {
            return new Cell()
            {
                CellValue = new CellValue(text),
                DataType = type
            };
        }
    }

    public class Loan
    {
        public decimal Amount { get; set; }
        public int Term { get; set; }
        public decimal Rate { get; set; }
        public DateTime Date { get; set; }
    }

    public class Payment
    {
        public int Number { get; set; }
        public DateTime Date { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal Principal { get; set; }
        public decimal Interest { get; set; }
        public decimal RemainingBalance { get; set; }
    }
}
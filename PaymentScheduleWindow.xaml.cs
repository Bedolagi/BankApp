using System.Collections.Generic;
using System.Windows;

namespace BankApp
{
    public partial class PaymentScheduleWindow : Window
    {
        public PaymentScheduleWindow(List<Payment> payments)
        {
            InitializeComponent();
            PaymentsDataGrid.ItemsSource = payments;
        }
    }
}
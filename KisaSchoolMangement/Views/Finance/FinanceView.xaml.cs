using KisaSchoolMangement.Models;
using KisaSchoolMangement.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace KisaSchoolMangement.Views.Finance
{
    public partial class FinanceView : Window
    {
        private readonly FinanceService _financeService;
        private ObservableCollection<FeeAssignmentModel> _assignments;
        private ObservableCollection<FeePaymentModel> _payments;
        private FeeAssignmentModel _selectedAssignment;
        private readonly int? _currentUserId;

        public FinanceView(int? currentUserId = null)
        {
            InitializeComponent();
            _financeService = new FinanceService();
            _currentUserId = currentUserId;
            dpPaymentDate.SelectedDate = DateTime.Now;
            LoadFinanceData();
        }

        private void LoadFinanceData()
        {
            LoadSummary();
            LoadAssignments();
            LoadPayments();
        }

        private void LoadSummary()
        {
            var summary = _financeService.GetFeeSummary();
            txtTotalAssigned.Text = summary.TotalAssigned.ToString("N0", CultureInfo.InvariantCulture);
            txtTotalCollected.Text = summary.TotalCollected.ToString("N0", CultureInfo.InvariantCulture);
            txtTotalPending.Text = summary.TotalPending.ToString("N0", CultureInfo.InvariantCulture);
        }

        private void LoadAssignments()
        {
            _assignments = _financeService.GetFeeAssignments();
            dgAssignments.ItemsSource = _assignments;
        }

        private void LoadPayments()
        {
            _payments = _financeService.GetFeePayments();
            dgPayments.ItemsSource = _payments;
        }

        private void DgAssignments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedAssignment = dgAssignments.SelectedItem as FeeAssignmentModel;

            if (_selectedAssignment != null)
            {
                txtSelectedStudent.Text = $"{_selectedAssignment.StudentName} (GR: {_selectedAssignment.GrNo})";
                txtPaymentAmount.Text = _selectedAssignment.Balance > 0
                    ? _selectedAssignment.Balance.ToString("N0", CultureInfo.InvariantCulture)
                    : "0";
            }
            else
            {
                txtSelectedStudent.Text = "No selection";
                txtPaymentAmount.Text = "";
            }
        }

        private void BtnCollectPayment_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAssignment == null)
            {
                MessageBox.Show("Please select an assignment to collect fee.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtPaymentAmount.Text, out var amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid payment amount.", "Invalid Amount",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var method = (cmbPaymentMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cash";
            var reference = txtTransactionRef.Text?.Trim() ?? "";
            var paymentDate = dpPaymentDate.SelectedDate ?? DateTime.Now;

            bool success = _financeService.AddFeePayment(_selectedAssignment.Id, paymentDate, amount, method, reference, _currentUserId);

            if (success)
            {
                MessageBox.Show("Payment recorded successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                txtTransactionRef.Text = "";
                LoadFinanceData();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadFinanceData();
        }
    }
}

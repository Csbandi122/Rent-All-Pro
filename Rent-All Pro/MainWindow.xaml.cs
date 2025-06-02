using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using RentAllPro.Models;

namespace RentAllPro
{
    public partial class MainWindow : Window
    {
        private Customer _currentCustomer;
        private Rental _currentRental;

        public MainWindow()
        {
            InitializeComponent();
            InitializeData();
        }

        private void InitializeData()
        {
            _currentCustomer = new Customer();
            _currentRental = new Rental();

            // Alapértelmezett értékek beállítása
            dpStartDate.SelectedDate = DateTime.Today;
            _currentRental.StartDate = DateTime.Today;

            CalculateExpectedReturnDate();
        }

        #region Validáció és számítások

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Csak számok engedélyezése
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TxtRentalDays_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CalculateExpectedReturnDate();
        }

        private void BtnOccupancy_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Foglaltság nyilvántartás funkció hamarosan elérhető!",
                "Információ",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            // TODO: Occupancy tracking window
        }

        private void DpStartDate_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dpStartDate.SelectedDate.HasValue)
            {
                _currentRental.StartDate = dpStartDate.SelectedDate.Value;
                CalculateExpectedReturnDate();
            }
        }

        private void CalculateExpectedReturnDate()
        {
            if (int.TryParse(txtRentalDays.Text, out int rentalDays) && rentalDays > 0)
            {
                _currentRental.RentalDays = rentalDays;
                _currentRental.ExpectedReturnDate = _currentRental.StartDate.AddDays(rentalDays);
                dpExpectedReturn.SelectedDate = _currentRental.ExpectedReturnDate;
            }
            else
            {
                dpExpectedReturn.SelectedDate = null;
            }
        }

        #endregion

        #region Adatok összegyűjtése

        private void CollectCustomerData()
        {
            _currentCustomer.FullName = txtFullName.Text.Trim();
            _currentCustomer.PostalCode = txtPostalCode.Text.Trim();
            _currentCustomer.City = txtCity.Text.Trim();
            _currentCustomer.Address = txtAddress.Text.Trim();
            _currentCustomer.Email = txtEmail.Text.Trim();
            _currentCustomer.IdNumber = txtIdNumber.Text.Trim();
        }

        private void CollectRentalData()
        {
            if (int.TryParse(txtRentalDays.Text, out int rentalDays))
            {
                _currentRental.RentalDays = rentalDays;
            }

            if (cmbPaymentMethod.SelectedItem != null)
            {
                _currentRental.PaymentMethod = ((System.Windows.Controls.ComboBoxItem)cmbPaymentMethod.SelectedItem).Content.ToString() ?? "";
            }

            if (dpStartDate.SelectedDate.HasValue)
            {
                _currentRental.StartDate = dpStartDate.SelectedDate.Value;
            }

            _currentRental.Notes = txtNotes.Text.Trim();
        }

        #endregion

        #region Validáció

        private bool ValidateForm()
        {
            var errors = new List<string>();

            // Ügyfél adatok validálása
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
                errors.Add("• Teljes név megadása kötelező");

            if (string.IsNullOrWhiteSpace(txtPostalCode.Text))
                errors.Add("• Irányítószám megadása kötelező");

            if (string.IsNullOrWhiteSpace(txtCity.Text))
                errors.Add("• Város megadása kötelező");

            if (string.IsNullOrWhiteSpace(txtAddress.Text))
                errors.Add("• Cím megadása kötelező");

            if (string.IsNullOrWhiteSpace(txtEmail.Text))
                errors.Add("• E-mail cím megadása kötelező");
            else if (!txtEmail.Text.Contains("@") || !txtEmail.Text.Contains("."))
                errors.Add("• Érvényes e-mail címet adjon meg");

            if (string.IsNullOrWhiteSpace(txtIdNumber.Text))
                errors.Add("• Igazolvány szám megadása kötelező");

            // Bérlési adatok validálása
            if (!int.TryParse(txtRentalDays.Text, out int rentalDays) || rentalDays <= 0)
                errors.Add("• Bérlési napok számának pozitív egész számnak kell lennie");

            if (cmbPaymentMethod.SelectedItem == null)
                errors.Add("• Fizetési mód kiválasztása kötelező");

            if (!dpStartDate.SelectedDate.HasValue)
                errors.Add("• Bérlés kezdő dátumának megadása kötelező");

            if (errors.Any())
            {
                MessageBox.Show(
                    "Kérjük javítsa a következő hibákat:\n\n" + string.Join("\n", errors),
                    "Hibaüzenet",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return false;
            }

            return true;
        }

        #endregion

        #region Gomb események

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                CollectCustomerData();
                CollectRentalData();

                MessageBox.Show(
                    $"Adatok sikeresen rögzítve!\n\n" +
                    $"Ügyfél: {_currentCustomer.FullName}\n" +
                    $"Bérlési napok: {_currentRental.RentalDays}\n" +
                    $"Fizetési mód: {_currentRental.PaymentMethod}\n" +
                    $"Várható visszavétel: {_currentRental.ExpectedReturnDate:yyyy.MM.dd}\n\n" +
                    $"Következő lépés: eszköz kiválasztás",
                    "Információ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Itt majd átmegyünk az eszköz kiválasztó ablakra
                // TODO: Equipment selection window
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Biztosan ki szeretne lépni? A megadott adatok elvesznek.",
                "Megerősítés",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        private void BtnStatistics_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Statisztika funkció hamarosan elérhető!",
                "Információ",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            // TODO: Statistics window
        }

        private void BtnEquipmentAdmin_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Eszköz adminisztráció funkció hamarosan elérhető!",
                "Információ",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            // TODO: Equipment administration window
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();  // <-- Settings helyett SettingsWindow
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        #endregion
    }
}
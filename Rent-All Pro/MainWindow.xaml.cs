using RentAllPro.Models;
using RentAllPro.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

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
            UpdateHeaderTitle(); // <-- Add ezt hozzá
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

                // Eszköz kiválasztó ablak megnyitása
                var equipmentSelectionWindow = new EquipmentSelectionWindow(_currentCustomer, _currentRental);
                equipmentSelectionWindow.Owner = this;

                var result = equipmentSelectionWindow.ShowDialog();

                if (result == true)
                {
                    // Bérlés sikeresen véglegesítve
                    MessageBox.Show(
                        "Bérlés sikeresen véglegesítve!\n\nKöszönjük a bizalmát!",
                        "Sikeres bérlés",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Form visszaállítása új bérléshez
                    //InitializeData();
                }
            }
        }
        public void UpdateHeaderTitle()
        {
            if (txtHeaderTitle != null)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.CompanyName))
                {
                    txtHeaderTitle.Text = Properties.Settings.Default.CompanyName;
                }
                else
                {
                    txtHeaderTitle.Text = "Eszközbérlés támogatás";
                }
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
            var equipmentWindow = new EquipmentAdministrationWindow();
            equipmentWindow.Owner = this;
            equipmentWindow.ShowDialog();
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
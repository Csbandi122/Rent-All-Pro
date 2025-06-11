using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using RentAllPro.Models;
using RentAllPro.Helpers;
using RentAllPro.Data;
using Microsoft.EntityFrameworkCore;

namespace RentAllPro.Windows
{
    public partial class EquipmentAdministrationWindow : Window
    {
        private List<Equipment> _allEquipments;
        private Equipment _currentEquipment;
        private bool _isEditMode;
        private string _originalImagePath;

        // Új változók az új funkciókhoz
        private List<Customer> _allCustomers;
        private List<Rental> _allRentals;

        public EquipmentAdministrationWindow()
        {
            InitializeComponent();
            InitializeData();
        }

        #region Inicializálás

        private async void InitializeData()
        {
            try
            {
                // Inicializálás
                _allEquipments = new List<Equipment>();
                _currentEquipment = new Equipment();
                _allCustomers = new List<Customer>();
                _allRentals = new List<Rental>();

                await LoadEquipments();
                await LoadCustomers();
                await LoadRentals();
                ClearForm();

                // Event handler-ek hozzáadása, ha a vezérlők léteznek
                if (txtSearch != null)
                {
                    txtSearch.GotFocus += TxtSearch_GotFocus;
                    txtSearch.LostFocus += TxtSearch_LostFocus;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Hiba az adatok betöltése során:\n{ex.Message}",
                    "Hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async Task LoadEquipments()
        {
            try
            {
                using (var context = new RentAllProContext())
                {
                    _allEquipments = await context.Equipments
                        .OrderBy(e => e.Type)
                        .ThenBy(e => e.Name)
                        .ToListAsync();
                }

                FilterAndDisplayEquipments();
            }
            catch (Exception ex)
            {
                throw new Exception($"Adatbázis hiba: {ex.Message}");
            }
        }

        private void FilterAndDisplayEquipments()
        {
            if (_allEquipments == null)
            {
                _allEquipments = new List<Equipment>();
            }

            var filteredEquipments = _allEquipments;

            if (!string.IsNullOrWhiteSpace(txtSearch?.Text) &&
                txtSearch.Text != "Keresés eszköz név vagy kód alapján...")
            {
                var searchTerm = txtSearch.Text.ToLower();
                filteredEquipments = _allEquipments.Where(e =>
                    e.Name.ToLower().Contains(searchTerm) ||
                    e.Code.ToLower().Contains(searchTerm) ||
                    e.Type.ToLower().Contains(searchTerm)
                ).ToList();
            }

            if (lstEquipments != null)
            {
                lstEquipments.ItemsSource = filteredEquipments;
            }
        }

        #endregion

        #region Keresés

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterAndDisplayEquipments();
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch?.Text == "Keresés eszköz név vagy kód alapján...")
            {
                txtSearch.Text = "";
                txtSearch.FontStyle = FontStyles.Normal;
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch != null && string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Keresés eszköz név vagy kód alapján...";
                txtSearch.FontStyle = FontStyles.Italic;
            }
        }

        #endregion

        #region Lista események

        private void LstEquipments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstEquipments?.SelectedItem is Equipment selectedEquipment)
            {
                LoadEquipmentToForm(selectedEquipment);
                if (btnDeleteEquipment != null)
                    btnDeleteEquipment.IsEnabled = true;
            }
            else
            {
                if (btnDeleteEquipment != null)
                    btnDeleteEquipment.IsEnabled = false;
            }
        }

        private void LoadEquipmentToForm(Equipment equipment)
        {
            if (equipment == null) return;

            _currentEquipment = equipment;
            _isEditMode = true;
            _originalImagePath = equipment.ImagePath;

            if (txtFormTitle != null)
                txtFormTitle.Text = $"📝 Eszköz szerkesztése: {equipment.Name}";

            // Adatok betöltése a form mezőibe - null ellenőrzésekkel
            if (txtType != null) txtType.Text = equipment.Type;
            if (txtName != null) txtName.Text = equipment.Name;
            if (txtCode != null) txtCode.Text = equipment.Code;
            if (txtValue != null) txtValue.Text = equipment.Value.ToString("0");
            if (txtDailyRate != null) txtDailyRate.Text = equipment.DailyRate.ToString("0");
            if (txtNotes != null) txtNotes.Text = equipment.Notes;
            if (chkIsAvailable != null) chkIsAvailable.IsChecked = equipment.IsAvailable;

            // Kép betöltése
            LoadEquipmentImage(equipment.ImagePath);
        }

        private void LoadEquipmentImage(string imagePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(imagePath))
                {
                    if (txtImagePath != null)
                        txtImagePath.Text = imagePath;

                    var bitmap = ImageHelper.LoadImageForDisplay(imagePath);
                    if (bitmap != null)
                    {
                        if (imgPreview != null)
                            imgPreview.Source = bitmap;
                        if (txtImageInfo != null)
                            txtImageInfo.Text = $"✅ Kép betöltve: {System.IO.Path.GetFileName(imagePath)}";
                    }
                    else
                    {
                        if (imgPreview != null)
                            imgPreview.Source = null;
                        if (txtImageInfo != null)
                            txtImageInfo.Text = "❌ Kép nem található vagy hibás formátum";
                    }
                }
                else
                {
                    if (txtImagePath != null) txtImagePath.Text = "";
                    if (imgPreview != null) imgPreview.Source = null;
                    if (txtImageInfo != null) txtImageInfo.Text = "💡 Kép feltöltése: 150px magas, szélesség arányosan";
                }
            }
            catch (Exception ex)
            {
                if (txtImageInfo != null)
                    txtImageInfo.Text = $"❌ Hiba a kép betöltésekor: {ex.Message}";
            }
        }

        #endregion

        #region Form műveletek

        private void ClearForm()
        {
            _currentEquipment = new Equipment();
            _isEditMode = false;
            _originalImagePath = null;

            if (txtFormTitle != null)
                txtFormTitle.Text = "📝 Új eszköz hozzáadása";

            // Form mezők törlése - null ellenőrzésekkel
            if (txtType != null) txtType.Text = "";
            if (txtName != null) txtName.Text = "";
            if (txtCode != null) txtCode.Text = "";
            if (txtValue != null) txtValue.Text = "";
            if (txtDailyRate != null) txtDailyRate.Text = "";
            if (txtNotes != null) txtNotes.Text = "";
            if (chkIsAvailable != null) chkIsAvailable.IsChecked = true;

            // Kép törlése
            if (txtImagePath != null) txtImagePath.Text = "";
            if (imgPreview != null) imgPreview.Source = null;
            if (txtImageInfo != null) txtImageInfo.Text = "💡 Kép feltöltése: 150px magas, szélesség arányosan";

            // Lista kijelölés törlése
            if (lstEquipments != null)
                lstEquipments.SelectedItem = null;
        }

        #endregion

        #region Validáció

        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.,]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool ValidateForm()
        {
            var errors = new List<string>();

            if (txtType == null || string.IsNullOrWhiteSpace(txtType.Text))
                errors.Add("• Eszköz típusa kötelező");

            if (txtName == null || string.IsNullOrWhiteSpace(txtName.Text))
                errors.Add("• Eszköz neve kötelező");

            if (txtCode == null || string.IsNullOrWhiteSpace(txtCode.Text))
                errors.Add("• Eszköz kódja kötelező");

            if (txtCode != null && !string.IsNullOrWhiteSpace(txtCode.Text) && _allEquipments != null)
            {
                var codeExists = _allEquipments.Any(e =>
                    e.Code.Equals(txtCode.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    e.Id != _currentEquipment?.Id);

                if (codeExists)
                    errors.Add("• Ez az eszköz kód már létezik");
            }

            if (txtValue == null || !decimal.TryParse(txtValue.Text.Replace(',', '.'), out decimal value) || value <= 0)
                errors.Add("• Eszköz értéke érvényes pozitív szám kell legyen");

            if (txtDailyRate == null || !decimal.TryParse(txtDailyRate.Text.Replace(',', '.'), out decimal dailyRate) || dailyRate <= 0)
                errors.Add("• Bérlési díj érvényes pozitív szám kell legyen");

            if (errors.Any())
            {
                MessageBox.Show(
                    "Kérjük javítsa a következő hibákat:\n\n" + string.Join("\n", errors),
                    "Hibás adatok",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return false;
            }

            return true;
        }

        #endregion

        #region Kép kezelés

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Eszköz kép kiválasztása",
                    Filter = "Képfájlok (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Minden fájl (*.*)|*.*",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var bitmap = ImageHelper.LoadImageForDisplay(openFileDialog.FileName);
                    if (bitmap != null)
                    {
                        if (imgPreview != null)
                            imgPreview.Source = bitmap;
                        if (txtImagePath != null)
                            txtImagePath.Text = openFileDialog.FileName;
                        if (txtImageInfo != null)
                            txtImageInfo.Text = $"✅ Kép kiválasztva: {System.IO.Path.GetFileName(openFileDialog.FileName)}";
                    }
                    else
                    {
                        MessageBox.Show(
                            "A kiválasztott fájl nem érvényes képformátum!",
                            "Hibás fájl",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Hiba a kép betöltése során:\n{ex.Message}",
                    "Hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void BtnRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            if (txtImagePath != null) txtImagePath.Text = "";
            if (imgPreview != null) imgPreview.Source = null;
            if (txtImageInfo != null) txtImageInfo.Text = "💡 Kép feltöltése: 150px magas, szélesség arányosan";
        }

        #endregion

        #region Gomb események

        private void BtnNewEquipment_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void BtnDeleteEquipment_Click(object sender, RoutedEventArgs e)
        {
            if (lstEquipments?.SelectedItem is Equipment selectedEquipment)
            {
                var result = MessageBox.Show(
                    $"Biztosan törölni szeretné a következő eszközt?\n\n" +
                    $"Név: {selectedEquipment.Name}\n" +
                    $"Kód: {selectedEquipment.Code}\n\n" +
                    $"Ez a művelet nem vonható vissza!",
                    "Eszköz törlése",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new RentAllProContext())
                        {
                            var equipmentToDelete = await context.Equipments.FindAsync(selectedEquipment.Id);
                            if (equipmentToDelete != null)
                            {
                                if (!string.IsNullOrEmpty(equipmentToDelete.ImagePath))
                                {
                                    try
                                    {
                                        ImageHelper.DeleteEquipmentImage(equipmentToDelete.ImagePath);
                                    }
                                    catch
                                    {
                                        // Ha nem sikerül törölni a képet, folytatjuk
                                    }
                                }

                                context.Equipments.Remove(equipmentToDelete);
                                await context.SaveChangesAsync();
                            }
                        }

                        MessageBox.Show("Eszköz sikeresen törölve!", "Siker");
                        await LoadEquipments();
                        ClearForm();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Hiba az eszköz törlése során:\n{ex.Message}", "Hiba");
                    }
                }
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                var equipment = _isEditMode ? _currentEquipment : new Equipment();

                if (equipment == null)
                    equipment = new Equipment();

                equipment.Type = txtType?.Text.Trim() ?? "";
                equipment.Name = txtName?.Text.Trim() ?? "";
                equipment.Code = txtCode?.Text.Trim() ?? "";
                equipment.Value = decimal.Parse(txtValue?.Text.Replace(',', '.') ?? "0");
                equipment.DailyRate = decimal.Parse(txtDailyRate?.Text.Replace(',', '.') ?? "0");
                equipment.Notes = txtNotes?.Text.Trim() ?? "";
                equipment.IsAvailable = chkIsAvailable?.IsChecked ?? true;

                var currentImagePath = txtImagePath?.Text ?? "";
                if (!string.IsNullOrEmpty(currentImagePath) && currentImagePath != _originalImagePath)
                {
                    var savedImagePath = ImageHelper.SaveEquipmentImage(currentImagePath, equipment.Code);

                    if (_isEditMode && !string.IsNullOrEmpty(_originalImagePath))
                    {
                        try
                        {
                            ImageHelper.DeleteEquipmentImage(_originalImagePath);
                        }
                        catch
                        {
                            // Ha nem sikerül törölni, folytatjuk
                        }
                    }

                    equipment.ImagePath = savedImagePath;
                }
                else if (string.IsNullOrEmpty(currentImagePath) && _isEditMode && !string.IsNullOrEmpty(_originalImagePath))
                {
                    try
                    {
                        ImageHelper.DeleteEquipmentImage(_originalImagePath);
                    }
                    catch
                    {
                        // Ha nem sikerül törölni, folytatjuk
                    }
                    equipment.ImagePath = null;
                }

                using (var context = new RentAllProContext())
                {
                    if (_isEditMode)
                    {
                        context.Equipments.Update(equipment);
                    }
                    else
                    {
                        equipment.CreatedAt = DateTime.Now;
                        await context.Equipments.AddAsync(equipment);
                    }

                    await context.SaveChangesAsync();
                }

                MessageBox.Show(
                    $"Eszköz sikeresen {(_isEditMode ? "frissítve" : "hozzáadva")}!",
                    "Siker"
                );

                await LoadEquipments();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az eszköz mentése során:\n{ex.Message}", "Hiba");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Ügyfelek kezelése

        private async Task LoadCustomers()
        {
            try
            {
                using (var context = new RentAllProContext())
                {
                    _allCustomers = await context.Customers
                        .OrderByDescending(c => c.CreatedAt)
                        .ToListAsync();
                }
                FilterAndDisplayCustomers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az ügyfelek betöltése során: {ex.Message}", "Hiba");
            }
        }

        private void FilterAndDisplayCustomers()
        {
            var filteredCustomers = _allCustomers;

            if (!string.IsNullOrWhiteSpace(txtCustomerSearch?.Text) &&
                txtCustomerSearch.Text != "Keresés név vagy email alapján...")
            {
                var searchTerm = txtCustomerSearch.Text.ToLower();
                filteredCustomers = _allCustomers.Where(c =>
                    c.FullName.ToLower().Contains(searchTerm) ||
                    c.Email.ToLower().Contains(searchTerm)
                ).ToList();
            }

            if (lstCustomers != null)
                lstCustomers.ItemsSource = filteredCustomers;
        }

        private void TxtCustomerSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterAndDisplayCustomers();
        }

        private void LstCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstCustomers?.SelectedItem is Customer selectedCustomer)
            {
                DisplayCustomerDetails(selectedCustomer);
                if (btnDeleteCustomer != null)
                    btnDeleteCustomer.IsEnabled = true;
            }
            else
            {
                if (btnDeleteCustomer != null)
                    btnDeleteCustomer.IsEnabled = false;
            }
        }

        private void DisplayCustomerDetails(Customer customer)
        {
            if (pnlCustomerDetails == null) return;

            pnlCustomerDetails.Children.Clear();

            var detailsGroup = new GroupBox { Header = "📋 Ügyfél adatok", Margin = new Thickness(0, 0, 0, 15) };
            var detailsPanel = new StackPanel();

            detailsPanel.Children.Add(CreateDetailRow("Név:", customer.FullName));
            detailsPanel.Children.Add(CreateDetailRow("Email:", customer.Email));
            detailsPanel.Children.Add(CreateDetailRow("Cím:", $"{customer.PostalCode} {customer.City}, {customer.Address}"));
            detailsPanel.Children.Add(CreateDetailRow("Igazolvány:", customer.IdNumber));
            detailsPanel.Children.Add(CreateDetailRow("Létrehozva:", customer.CreatedAt.ToString("yyyy.MM.dd HH:mm")));

            detailsGroup.Content = detailsPanel;
            pnlCustomerDetails.Children.Add(detailsGroup);

            LoadCustomerRentals(customer.Id);
        }

        private StackPanel CreateDetailRow(string label, string value)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

            var labelBlock = new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Bold,
                Width = 100,
                VerticalAlignment = VerticalAlignment.Top
            };

            var valueBlock = new TextBlock
            {
                Text = value,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            };

            panel.Children.Add(labelBlock);
            panel.Children.Add(valueBlock);

            return panel;
        }

        private async void LoadCustomerRentals(int customerId)
        {
            try
            {
                using (var context = new RentAllProContext())
                {
                    var customerRentals = await context.Rentals
                        .Where(r => r.CustomerId == customerId)
                        .OrderByDescending(r => r.CreatedAt)
                        .ToListAsync();

                    if (customerRentals.Any())
                    {
                        var rentalsGroup = new GroupBox { Header = "📋 Bérlési előzmények", Margin = new Thickness(0, 15, 0, 0) };
                        var rentalsPanel = new StackPanel();

                        foreach (var rental in customerRentals.Take(5))
                        {
                            var rentalInfo = $"#{rental.Id} - {rental.StartDate:yyyy.MM.dd} ({rental.RentalDays} nap) - {rental.TotalAmount:N0} Ft - {rental.Status}";
                            var rentalBlock = new TextBlock
                            {
                                Text = rentalInfo,
                                Margin = new Thickness(0, 2, 0, 2),
                                FontSize = 11
                            };
                            rentalsPanel.Children.Add(rentalBlock);
                        }

                        if (customerRentals.Count > 5)
                        {
                            var moreBlock = new TextBlock
                            {
                                Text = $"... és még {customerRentals.Count - 5} bérlés",
                                FontStyle = FontStyles.Italic,
                                Foreground = Brushes.Gray,
                                Margin = new Thickness(0, 5, 0, 0)
                            };
                            rentalsPanel.Children.Add(moreBlock);
                        }

                        rentalsGroup.Content = rentalsPanel;
                        pnlCustomerDetails.Children.Add(rentalsGroup);
                    }
                }
            }
            catch (Exception ex)
            {
                // Hiba esetén csak nem jelenítjük meg a bérléseket
            }
        }

        private async void BtnDeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (lstCustomers?.SelectedItem is Customer selectedCustomer)
            {
                using (var context = new RentAllProContext())
                {
                    var activeRentals = await context.Rentals
                        .Where(r => r.CustomerId == selectedCustomer.Id && r.Status == "Active")
                        .CountAsync();

                    if (activeRentals > 0)
                    {
                        MessageBox.Show(
                            $"Az ügyfélnek {activeRentals} db aktív bérlése van!\n\nElőbb zárja le a bérléseket.",
                            "Nem törölhető"
                        );
                        return;
                    }
                }

                var result = MessageBox.Show(
                    $"Biztosan törölni szeretné a következő ügyfelet?\n\n" +
                    $"Név: {selectedCustomer.FullName}\n" +
                    $"Email: {selectedCustomer.Email}\n\n" +
                    $"FIGYELEM: Az összes bérlési előzménye is törlődik!\n" +
                    $"Ez a művelet nem vonható vissza!",
                    "Ügyfél törlése",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new RentAllProContext())
                        {
                            var customerRentals = await context.Rentals
                                .Where(r => r.CustomerId == selectedCustomer.Id)
                                .ToListAsync();

                            foreach (var rental in customerRentals)
                            {
                                var rentalEquipments = await context.RentalEquipments
                                    .Where(re => re.RentalId == rental.Id)
                                    .ToListAsync();

                                context.RentalEquipments.RemoveRange(rentalEquipments);
                            }

                            context.Rentals.RemoveRange(customerRentals);

                            var customerToDelete = await context.Customers.FindAsync(selectedCustomer.Id);
                            if (customerToDelete != null)
                            {
                                context.Customers.Remove(customerToDelete);
                            }

                            await context.SaveChangesAsync();
                        }

                        MessageBox.Show("Ügyfél és minden kapcsolódó adata sikeresen törölve!", "Siker");
                        await LoadCustomers();
                        await LoadRentals();

                        if (pnlCustomerDetails != null)
                        {
                            pnlCustomerDetails.Children.Clear();
                            pnlCustomerDetails.Children.Add(new TextBlock
                            {
                                Text = "Válasszon ki egy ügyfelet a részletek megtekintéséhez.",
                                FontStyle = FontStyles.Italic,
                                Foreground = Brushes.Gray,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Hiba az ügyfél törlése során: {ex.Message}", "Hiba");
                    }
                }
            }
        }

        #endregion

        #region Bérlések kezelése

        private async Task LoadRentals()
        {
            try
            {
                using (var context = new RentAllProContext())
                {
                    _allRentals = await context.Rentals
                        .Include(r => r.Customer)
                        .OrderByDescending(r => r.CreatedAt)
                        .ToListAsync();
                }
                FilterAndDisplayRentals();
                UpdateRentalCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a bérlések betöltése során: {ex.Message}", "Hiba");
            }
        }

        private void FilterAndDisplayRentals()
        {
            var filteredRentals = _allRentals;

            if (!string.IsNullOrWhiteSpace(txtRentalSearch?.Text) &&
                txtRentalSearch.Text != "Keresés ügyfél név alapján...")
            {
                var searchTerm = txtRentalSearch.Text.ToLower();
                filteredRentals = filteredRentals.Where(r =>
                    r.Customer?.FullName?.ToLower().Contains(searchTerm) == true
                ).ToList();
            }

            if (cmbRentalStatus?.SelectedItem is ComboBoxItem selectedStatus &&
                selectedStatus.Content.ToString() != "Minden állapot")
            {
                var statusFilter = selectedStatus.Content.ToString();
                filteredRentals = filteredRentals.Where(r => r.Status == statusFilter).ToList();
            }

            if (lstRentals != null)
                lstRentals.ItemsSource = filteredRentals;
        }

        private void UpdateRentalCount()
        {
            if (txtRentalCount != null && _allRentals != null)
            {
                var activeCount = _allRentals.Count(r => r.Status == "Active");
                var totalCount = _allRentals.Count;
                txtRentalCount.Text = $"Bérlések száma: {totalCount} (ebből aktív: {activeCount})";
            }
        }
        private async void BtnDeleteRental_Click(object sender, RoutedEventArgs e)
        {
            if (lstRentals?.SelectedItem is Rental selectedRental)
            {
                var result = MessageBox.Show(
                    $"Biztosan törölni szeretné a következő bérlést?\n\n" +
                    $"Bérlés ID: {selectedRental.Id}\n" +
                    $"Ügyfél: {selectedRental.Customer?.FullName}\n" +
                    $"Összeg: {selectedRental.TotalAmount:N0} Ft\n\n" +
                    $"FIGYELEM: Ez pénzügyi tranzakció törlése!\n" +
                    $"Ez a művelet nem vonható vissza!",
                    "Bérlés törlése",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new RentAllProContext())
                        {
                            // Kapcsolódó RentalEquipments törlése előbb
                            var rentalEquipments = await context.RentalEquipments
                                .Where(re => re.RentalId == selectedRental.Id)
                                .ToListAsync();

                            context.RentalEquipments.RemoveRange(rentalEquipments);

                            // Bérlés törlése
                            var rentalToDelete = await context.Rentals.FindAsync(selectedRental.Id);
                            if (rentalToDelete != null)
                            {
                                context.Rentals.Remove(rentalToDelete);
                            }

                            await context.SaveChangesAsync();
                        }

                        MessageBox.Show("Bérlés sikeresen törölve!", "Siker");
                        await LoadRentals();
                        await LoadCustomers(); // Frissíti az ügyfél részleteket is
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Hiba a bérlés törlése során: {ex.Message}", "Hiba");
                    }
                }
            }
        }

        private void TxtRentalSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterAndDisplayRentals();
        }

        private void CmbRentalStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterAndDisplayRentals();
        }

        private void LstRentals_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstRentals?.SelectedItem is Rental selectedRental)
            {
                if (btnDeleteRental != null)
                    btnDeleteRental.IsEnabled = true;
                // Itt később lehetne bérlés részleteket megjeleníteni
            }
            else
            {
                if (btnDeleteRental != null)
                    btnDeleteRental.IsEnabled = false;
            }
        }

        #endregion
    }
}
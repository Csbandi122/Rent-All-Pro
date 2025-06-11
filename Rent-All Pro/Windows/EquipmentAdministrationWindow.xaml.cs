using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using RentAllPro.Data;
using RentAllPro.Helpers;
using RentAllPro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RentAllPro.Windows
{
    public partial class EquipmentAdministrationWindow : Window
    {
        private List<Equipment> _allEquipments;
        private Equipment _currentEquipment;
        private bool _isEditMode;
        private string _originalImagePath;

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

                await LoadEquipments();
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
            // Null check hozzáadása
            if (_allEquipments == null)
            {
                _allEquipments = new List<Equipment>();
            }

            var filteredEquipments = _allEquipments;

            // Keresési szűrő alkalmazása
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

            // ListView null check
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
            if (txtSearch != null && txtSearch.Text == "Keresés eszköz név vagy kód alapján...")
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
            // Decimális számok és vessző/pont engedélyezése
            Regex regex = new Regex("[^0-9.,]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool ValidateForm()
        {
            var errors = new List<string>();

            // Kötelező mezők ellenőrzése - null ellenőrzésekkel
            if (txtType == null || string.IsNullOrWhiteSpace(txtType.Text))
                errors.Add("• Eszköz típusa kötelező");

            if (txtName == null || string.IsNullOrWhiteSpace(txtName.Text))
                errors.Add("• Eszköz neve kötelező");

            if (txtCode == null || string.IsNullOrWhiteSpace(txtCode.Text))
                errors.Add("• Eszköz kódja kötelező");

            // Eszköz kód egyediségének ellenőrzése
            if (txtCode != null && !string.IsNullOrWhiteSpace(txtCode.Text) && _allEquipments != null)
            {
                var codeExists = _allEquipments.Any(e =>
                    e.Code.Equals(txtCode.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    e.Id != _currentEquipment?.Id);

                if (codeExists)
                    errors.Add("• Ez az eszköz kód már létezik");
            }

            // Pénzügyi adatok ellenőrzése
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
                    // Kép előnézet betöltése az eredeti fájlból
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
                                // Kép törlése a fájlrendszerből - null ellenőrzéssel és hibakezeléssel
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

                        MessageBox.Show(
                            "Eszköz sikeresen törölve!",
                            "Siker",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );

                        await LoadEquipments();
                        ClearForm();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Hiba az eszköz törlése során:\n{ex.Message}",
                            "Hiba",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
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
                // Form adatok összegyűjtése
                var equipment = _isEditMode ? _currentEquipment : new Equipment();

                if (equipment == null)
                    equipment = new Equipment();

                // Null ellenőrzések a form mezőknél
                equipment.Type = txtType?.Text.Trim() ?? "";
                equipment.Name = txtName?.Text.Trim() ?? "";
                equipment.Code = txtCode?.Text.Trim() ?? "";
                equipment.Value = decimal.Parse(txtValue?.Text.Replace(',', '.') ?? "0");
                equipment.DailyRate = decimal.Parse(txtDailyRate?.Text.Replace(',', '.') ?? "0");
                equipment.Notes = txtNotes?.Text.Trim() ?? "";
                equipment.IsAvailable = chkIsAvailable?.IsChecked ?? true;

                // Kép kezelése
                var currentImagePath = txtImagePath?.Text ?? "";
                if (!string.IsNullOrEmpty(currentImagePath) && currentImagePath != _originalImagePath)
                {
                    // Új kép mentése
                    var savedImagePath = ImageHelper.SaveEquipmentImage(currentImagePath, equipment.Code);

                    // Régi kép törlése (ha volt és szerkesztés módban vagyunk)
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
                    // Kép eltávolítása
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

                // Adatbázisba mentés
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
                    "Siker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                await LoadEquipments();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Hiba az eszköz mentése során:\n{ex.Message}",
                    "Hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using RentAllPro.Models;
using RentAllPro.Helpers;
using RentAllPro.Data;
using Microsoft.EntityFrameworkCore;

namespace RentAllPro.Windows
{
    public partial class EquipmentAdministrationWindow : Window  // ← Ez volt a probléma!
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
            if (txtSearch.Text == "Keresés eszköz név vagy kód alapján...")
            {
                txtSearch.Text = "";
                txtSearch.FontStyle = FontStyles.Normal;
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Keresés eszköz név vagy kód alapján...";
                txtSearch.FontStyle = FontStyles.Italic;
            }
        }

        #endregion

        #region Lista események

        private void LstEquipments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstEquipments.SelectedItem is Equipment selectedEquipment)
            {
                LoadEquipmentToForm(selectedEquipment);
                btnDeleteEquipment.IsEnabled = true;
            }
            else
            {
                btnDeleteEquipment.IsEnabled = false;
            }
        }

        private void LoadEquipmentToForm(Equipment equipment)
        {
            _currentEquipment = equipment;
            _isEditMode = true;
            _originalImagePath = equipment.ImagePath;

            txtFormTitle.Text = $"📝 Eszköz szerkesztése: {equipment.Name}";

            // Adatok betöltése a form mezőibe
            txtType.Text = equipment.Type;
            txtName.Text = equipment.Name;
            txtCode.Text = equipment.Code;
            txtValue.Text = equipment.Value.ToString("0");
            txtDailyRate.Text = equipment.DailyRate.ToString("0");
            txtNotes.Text = equipment.Notes;
            chkIsAvailable.IsChecked = equipment.IsAvailable;

            // Kép betöltése
            LoadEquipmentImage(equipment.ImagePath);
        }

        private void LoadEquipmentImage(string imagePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(imagePath))
                {
                    txtImagePath.Text = imagePath;
                    var bitmap = ImageHelper.LoadImageForDisplay(imagePath);
                    if (bitmap != null)
                    {
                        imgPreview.Source = bitmap;
                        txtImageInfo.Text = $"✅ Kép betöltve: {System.IO.Path.GetFileName(imagePath)}";
                    }
                    else
                    {
                        imgPreview.Source = null;
                        txtImageInfo.Text = "❌ Kép nem található vagy hibás formátum";
                    }
                }
                else
                {
                    txtImagePath.Text = "";
                    imgPreview.Source = null;
                    txtImageInfo.Text = "💡 Kép feltöltése: 150px magas, szélesség arányosan";
                }
            }
            catch (Exception ex)
            {
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

            txtFormTitle.Text = "📝 Új eszköz hozzáadása";

            // Form mezők törlése
            txtType.Text = "";
            txtName.Text = "";
            txtCode.Text = "";
            txtValue.Text = "";
            txtDailyRate.Text = "";
            txtNotes.Text = "";
            chkIsAvailable.IsChecked = true;

            // Kép törlése
            txtImagePath.Text = "";
            imgPreview.Source = null;
            txtImageInfo.Text = "💡 Kép feltöltése: 150px magas, szélesség arányosan";

            // Lista kijelölés törlése
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

            // Kötelező mezők ellenőrzése
            if (string.IsNullOrWhiteSpace(txtType.Text))
                errors.Add("• Eszköz típusa kötelező");

            if (string.IsNullOrWhiteSpace(txtName.Text))
                errors.Add("• Eszköz neve kötelező");

            if (string.IsNullOrWhiteSpace(txtCode.Text))
                errors.Add("• Eszköz kódja kötelező");

            // Eszköz kód egyediségének ellenőrzése
            if (!string.IsNullOrWhiteSpace(txtCode.Text))
            {
                var codeExists = _allEquipments.Any(e =>
                    e.Code.Equals(txtCode.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    e.Id != _currentEquipment.Id);

                if (codeExists)
                    errors.Add("• Ez az eszköz kód már létezik");
            }

            // Pénzügyi adatok ellenőrzése
            if (!decimal.TryParse(txtValue.Text.Replace(',', '.'), out decimal value) || value <= 0)
                errors.Add("• Eszköz értéke érvényes pozitív szám kell legyen");

            if (!decimal.TryParse(txtDailyRate.Text.Replace(',', '.'), out decimal dailyRate) || dailyRate <= 0)
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
                        imgPreview.Source = bitmap;
                        txtImagePath.Text = openFileDialog.FileName;
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
            txtImagePath.Text = "";
            imgPreview.Source = null;
            txtImageInfo.Text = "💡 Kép feltöltése: 150px magas, szélesség arányosan";
        }

        #endregion

        #region Gomb események

        private void BtnNewEquipment_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void BtnDeleteEquipment_Click(object sender, RoutedEventArgs e)
        {
            if (lstEquipments.SelectedItem is Equipment selectedEquipment)
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
                                // Kép törlése a fájlrendszerből
                                if (!string.IsNullOrEmpty(equipmentToDelete.ImagePath))
                                {
                                    ImageHelper.DeleteEquipmentImage(equipmentToDelete.ImagePath);
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

                equipment.Type = txtType.Text.Trim();
                equipment.Name = txtName.Text.Trim();
                equipment.Code = txtCode.Text.Trim();
                equipment.Value = decimal.Parse(txtValue.Text.Replace(',', '.'));
                equipment.DailyRate = decimal.Parse(txtDailyRate.Text.Replace(',', '.'));
                equipment.Notes = txtNotes.Text.Trim();
                equipment.IsAvailable = chkIsAvailable.IsChecked ?? true;

                // Kép kezelése
                if (!string.IsNullOrEmpty(txtImagePath.Text) && txtImagePath.Text != _originalImagePath)
                {
                    // Új kép mentése
                    var savedImagePath = ImageHelper.SaveEquipmentImage(txtImagePath.Text, equipment.Code);

                    // Régi kép törlése (ha volt és szerkesztés módban vagyunk)
                    if (_isEditMode && !string.IsNullOrEmpty(_originalImagePath))
                    {
                        ImageHelper.DeleteEquipmentImage(_originalImagePath);
                    }

                    equipment.ImagePath = savedImagePath;
                }
                else if (string.IsNullOrEmpty(txtImagePath.Text) && _isEditMode && !string.IsNullOrEmpty(_originalImagePath))
                {
                    // Kép eltávolítása
                    ImageHelper.DeleteEquipmentImage(_originalImagePath);
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
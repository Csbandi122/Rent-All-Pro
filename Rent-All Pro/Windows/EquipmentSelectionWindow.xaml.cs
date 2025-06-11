using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using RentAllPro.Models;
using RentAllPro.Data;
using RentAllPro.Helpers;

namespace RentAllPro.Windows
{
    public partial class EquipmentSelectionWindow : Window
    {
        private List<Equipment> _allEquipments;
        private List<Equipment> _selectedEquipments;
        private Customer _customer;
        private Rental _rental;

        public List<Equipment> SelectedEquipments => _selectedEquipments;

        public EquipmentSelectionWindow(Customer customer, Rental rental)
        {
            InitializeComponent();
            _customer = customer;
            _rental = rental;
            _selectedEquipments = new List<Equipment>();

            InitializeData();
        }

        #region Inicializálás

        private async void InitializeData()
        {
            try
            {
                // Ügyfél és bérlési adatok megjelenítése
                DisplayCustomerAndRentalInfo();

                // Keresési mezők inicializálása
                InitializeSearchFields();

                // Eszközök betöltése
                await LoadEquipments();

                // Kezdeti megjelenítés
                DisplayEquipments();
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

        private void DisplayCustomerAndRentalInfo()
        {
            txtCustomerInfo.Text = $"Ügyfél: {_customer.FullName}";
            txtRentalDaysInfo.Text = $"Bérlési napok: {_rental.RentalDays}";
            txtDateInfo.Text = $"{_rental.StartDate:yyyy.MM.dd} - {_rental.ExpectedReturnDate:yyyy.MM.dd}";
        }

        private void InitializeSearchFields()
        {
            // Keresési mező event handler-ek
            txtSearch.GotFocus += TxtSearch_GotFocus;
            txtSearch.LostFocus += TxtSearch_LostFocus;
        }

        private async Task LoadEquipments()
        {
            try
            {
                using (var context = new RentAllProContext())
                {
                    _allEquipments = await context.Equipments
                        .Where(e => e.IsAvailable) // Csak elérhető eszközök alapból
                        .OrderBy(e => e.Type)
                        .ThenBy(e => e.Name)
                        .ToListAsync();
                }

                // Típusok betöltése a szűrő ComboBox-ba
                LoadEquipmentTypes();
            }
            catch (Exception ex)
            {
                throw new Exception($"Adatbázis hiba: {ex.Message}");
            }
        }

        private void LoadEquipmentTypes()
        {
            if (_allEquipments?.Any() == true)
            {
                var types = _allEquipments
                    .Select(e => e.Type)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                // Meglévő típusok törlése (kivéve az első "Minden típus" elemet)
                for (int i = cmbTypeFilter.Items.Count - 1; i > 0; i--)
                {
                    cmbTypeFilter.Items.RemoveAt(i);
                }

                // Új típusok hozzáadása
                foreach (var type in types)
                {
                    cmbTypeFilter.Items.Add(new ComboBoxItem { Content = type });
                }
            }
        }

        #endregion

        #region Eszközök megjelenítése

        private void DisplayEquipments()
        {
            if (_allEquipments == null)
                return;

            // Szűrt eszközök lekérése
            var filteredEquipments = GetFilteredEquipments();

            // Meglévő elemek törlése
            pnlEquipments.Children.Clear();

            // Eszköz kártyák létrehozása
            foreach (var equipment in filteredEquipments)
            {
                var card = CreateEquipmentCard(equipment);
                pnlEquipments.Children.Add(card);
            }

            // Ha nincs eredmény
            if (!filteredEquipments.Any())
            {
                var noResultsText = new TextBlock
                {
                    Text = "Nincs a feltételeknek megfelelő eszköz.",
                    FontSize = 16,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(20)
                };
                pnlEquipments.Children.Add(noResultsText);
            }
        }

        private List<Equipment> GetFilteredEquipments()
        {
            var equipments = _allEquipments;

            // Elérhetőség szűrés
            if (chkOnlyAvailable.IsChecked == true)
            {
                equipments = equipments.Where(e => e.IsAvailable).ToList();
            }

            // Típus szűrés
            if (cmbTypeFilter.SelectedItem is ComboBoxItem selectedType &&
                selectedType.Content.ToString() != "Minden típus")
            {
                equipments = equipments.Where(e => e.Type == selectedType.Content.ToString()).ToList();
            }

            // Keresési szűrés
            if (!string.IsNullOrWhiteSpace(txtSearch.Text) &&
                txtSearch.Text != "Keresés eszköz név, típus vagy kód alapján...")
            {
                var searchTerm = txtSearch.Text.ToLower();
                equipments = equipments.Where(e =>
                    e.Name.ToLower().Contains(searchTerm) ||
                    e.Type.ToLower().Contains(searchTerm) ||
                    e.Code.ToLower().Contains(searchTerm)
                ).ToList();
            }

            return equipments;
        }

        private Border CreateEquipmentCard(Equipment equipment)
        {
            // Kártya konténer
            var card = new Border
            {
                Width = 260,
                Height = 300,
                Tag = equipment
            };

            // Stílus beállítása (kiválasztott vagy normál)
            var isSelected = _selectedEquipments.Any(e => e.Id == equipment.Id);
            card.Style = (Style)FindResource(isSelected ? "SelectedEquipmentCard" : "EquipmentCard");

            // Kattintás event
            card.MouseLeftButtonUp += (s, e) => ToggleEquipmentSelection(equipment);

            // Kártya tartalom
            var stackPanel = new StackPanel();

            // Kép
            var image = new Image
            {
                Height = 120,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 0, 5)
            };

            // Kép betöltése
            if (!string.IsNullOrEmpty(equipment.ImagePath))
            {
                var bitmap = ImageHelper.LoadImageForDisplay(equipment.ImagePath);
                if (bitmap != null)
                {
                    image.Source = bitmap;
                }
                else
                {
                    // Placeholder kép hiányzó kép esetén
                    image.Source = CreatePlaceholderImage();
                }
            }
            else
            {
                image.Source = CreatePlaceholderImage();
            }

            stackPanel.Children.Add(image);

            // Eszköz neve
            var nameText = new TextBlock
            {
                Text = equipment.Name,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 5, 0, 2),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackPanel.Children.Add(nameText);

            // Eszköz típusa
            var typeText = new TextBlock
            {
                Text = equipment.Type,
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackPanel.Children.Add(typeText);

            // Kód
            var codeText = new TextBlock
            {
                Text = $"Kód: {equipment.Code}",
                FontSize = 10,
                Foreground = Brushes.DarkGray,
                Margin = new Thickness(0, 0, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackPanel.Children.Add(codeText);

            // Ár információ
            var pricePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var dailyRateText = new TextBlock
            {
                Text = $"{equipment.DailyRate:N0} Ft/nap",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(46, 134, 171))
            };
            pricePanel.Children.Add(dailyRateText);

            stackPanel.Children.Add(pricePanel);

            // Teljes ár a bérlési napokra
            var totalPrice = equipment.DailyRate * _rental.RentalDays;
            var totalPriceText = new TextBlock
            {
                Text = $"Összesen: {totalPrice:N0} Ft",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                Margin = new Thickness(0, 2, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackPanel.Children.Add(totalPriceText);

            // Kiválasztás jelző (ha ki van választva)
            if (isSelected)
            {
                var selectedIcon = new TextBlock
                {
                    Text = "✅ KIVÁLASZTVA",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                    Margin = new Thickness(0, 5, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                stackPanel.Children.Add(selectedIcon);
            }

            card.Child = stackPanel;
            return card;
        }

        private ImageSource CreatePlaceholderImage()
        {
            // Egyszerű placeholder kép létrehozása szöveggel
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Brushes.LightGray, null, new Rect(0, 0, 120, 120));
                drawingContext.DrawText(
                    new FormattedText("Nincs kép",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        12,
                        Brushes.DarkGray,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip),
                    new Point(30, 55)
                );
            }

            var renderTargetBitmap = new RenderTargetBitmap(120, 120, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            return renderTargetBitmap;
        }

        #endregion

        #region Eszköz kiválasztás

        private void ToggleEquipmentSelection(Equipment equipment)
        {
            var isCurrentlySelected = _selectedEquipments.Any(e => e.Id == equipment.Id);

            if (isCurrentlySelected)
            {
                // Kiválasztás megszüntetése
                _selectedEquipments.RemoveAll(e => e.Id == equipment.Id);
            }
            else
            {
                // Kiválasztás hozzáadása
                _selectedEquipments.Add(equipment);
            }

            // Megjelenítés frissítése
            DisplayEquipments();
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            var selectedCount = _selectedEquipments.Count;
            var totalAmount = _selectedEquipments.Sum(e => e.DailyRate * _rental.RentalDays);

            txtSelectedCount.Text = $"Kiválasztott eszközök: {selectedCount} db";
            txtTotalAmount.Text = $"Összesen: {totalAmount:N0} Ft";

            // Tovább gomb engedélyezése/letiltása
            btnContinue.IsEnabled = selectedCount > 0;
        }

        #endregion

        #region Keresés és szűrés

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            DisplayEquipments();
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Keresés eszköz név, típus vagy kód alapján...")
            {
                txtSearch.Text = "";
                txtSearch.FontStyle = FontStyles.Normal;
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Keresés eszköz név, típus vagy kód alapján...";
                txtSearch.FontStyle = FontStyles.Italic;
            }
        }

        private void CmbTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DisplayEquipments();
        }

        private void ChkOnlyAvailable_CheckedChanged(object sender, RoutedEventArgs e)
        {
            DisplayEquipments();
        }

        #endregion

        #region Navigáció

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEquipments.Count == 0)
            {
                MessageBox.Show(
                    "Kérjük válasszon ki legalább egy eszközt!",
                    "Nincs kiválasztott eszköz",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Összeg beállítása a rental objektumban
            _rental.TotalAmount = _selectedEquipments.Sum(e => e.DailyRate * _rental.RentalDays);

            this.DialogResult = true;
            this.Close();
        }

        #endregion
    }
}
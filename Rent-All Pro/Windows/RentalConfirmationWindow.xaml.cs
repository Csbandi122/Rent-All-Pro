using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using RentAllPro.Models;
using RentAllPro.Data;
using RentAllPro.Services;

namespace RentAllPro.Windows
{
    public partial class RentalConfirmationWindow : Window
    {
        private Customer _customer;
        private Rental _rental;
        private List<Equipment> _selectedEquipments;
        private string _tempWordPath;
        private string _finalWordPath;
        private PdfService _pdfService;

        public RentalConfirmationWindow(Customer customer, Rental rental, List<Equipment> selectedEquipments)
        {
            InitializeComponent();
            _customer = customer;
            _rental = rental;
            _selectedEquipments = selectedEquipments;
            _pdfService = new PdfService();

            InitializeData();
        }

        #region Inicializálás

        private void InitializeData()
        {
            try
            {
                DisplayCustomerData();
                DisplayRentalData();
                DisplaySelectedEquipments();

                // PDF fájl útvonalak előkészítése
                PrepareWordPaths();
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

        private void DisplayCustomerData()
        {
            txtCustomerInfo.Text = $"Ügyfél: {_customer.FullName}";
            txtCustomerName.Text = _customer.FullName;
            txtCustomerAddress.Text = $"{_customer.PostalCode} {_customer.City}, {_customer.Address}";
            txtCustomerContact.Text = $"{_customer.Email} / {_customer.IdNumber}";
        }

        private void DisplayRentalData()
        {
            txtStartDate.Text = _rental.StartDate.ToString("yyyy.MM.dd");
            txtRentalPeriod.Text = $"{_rental.RentalDays} nap";
            txtPaymentMethod.Text = _rental.PaymentMethod;
            txtDateInfo.Text = $"{_rental.StartDate:yyyy.MM.dd} - {_rental.ExpectedReturnDate:yyyy.MM.dd}";
            txtTotalAmount.Text = $"{_rental.TotalAmount:N0} Ft";
        }

        private void DisplaySelectedEquipments()
        {
            // Equipments with calculated total price for display
            var equipmentDisplayList = _selectedEquipments.Select(e => new
            {
                e.Code,
                e.Name,
                e.Type,
                e.DailyRate,
                TotalPrice = e.DailyRate * _rental.RentalDays
            }).ToList();

            lstSelectedEquipments.ItemsSource = equipmentDisplayList;
        }

        private void PrepareWordPaths()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var customerNameSafe = _customer.FullName.Replace(" ", "").Replace(".", "");

            // Temp Word a Windows TEMP mappában
            var tempFolder = Path.Combine(Path.GetTempPath(), "RentAllPro");
            Directory.CreateDirectory(tempFolder);
            _tempWordPath = Path.Combine(tempFolder, $"szerződés_temp_{timestamp}.docx");

            // Végleges Word a projekt mappában
            var contractsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos", "Contracts");
            Directory.CreateDirectory(contractsFolder);
            _finalWordPath = Path.Combine(contractsFolder, $"szerződés_{customerNameSafe}_{timestamp}.docx");
        }

        #endregion

        #region PDF kezelés

        private async void BtnGeneratePDF_Click(object sender, RoutedEventArgs e)
        {
            btnGeneratePDF.IsEnabled = false;
            btnGeneratePDF.Content = "📄 Generálás...";

            try
            {
                var result = await _pdfService.GenerateContractPdfAsync(_customer, _rental, _selectedEquipments, _tempWordPath);

                if (result.Success)
                {
                    _tempWordPath = result.WordFilePath; // Word fájl elérési útja
                    btnOpenPDF.IsEnabled = true;

                    MessageBox.Show(
                        "Word szerződés sikeresen generálva!\n\n" +
                        "Most nyissa meg aláírásra Word-ben.",
                        "Szerződés generálás sikeres",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        $"Hiba a szerződés generálás során:\n\n{result.ErrorMessage}",
                        "Generálás hiba",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Váratlan hiba történt:\n{ex.Message}",
                    "Hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                btnGeneratePDF.IsEnabled = true;
                btnGeneratePDF.Content = "📄 Word szerződés generálása";
            }
        }

        private void BtnOpenPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(_tempWordPath))
                {
                    MessageBox.Show(
                        "A Word fájl nem található! Kérjük generálja újra.",
                        "Fájl nem található",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // Word megnyitása
                Process.Start(new ProcessStartInfo
                {
                    FileName = _tempWordPath,
                    UseShellExecute = true
                });

                // Checkbox engedélyezése
                chkContractSigned.IsEnabled = true;

                MessageBox.Show(
                    "Word szerződés megnyitva!\n\n" +
                    "1. Írja alá a szerződést Word-ben\n" +
                    "2. Mentse el PDF-ként (Fájl → Mentés másként → PDF)\n" +
                    "3. Jelölje be az alábbi jelölőnégyzetet",
                    "Szerződés megnyitva",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Hiba a Word megnyitása során:\n{ex.Message}",
                    "Hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void ChkContractSigned_Checked(object sender, RoutedEventArgs e)
        {
            btnFinalize.IsEnabled = chkContractSigned.IsChecked == true;
        }

        #endregion

        #region Bérlés véglegesítése

        private async void BtnFinalize_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Biztosan véglegesíti a bérlést?\n\n" +
                $"Ügyfél: {_customer.FullName}\n" +
                $"Eszközök: {_selectedEquipments.Count} db\n" +
                $"Összeg: {_rental.TotalAmount:N0} Ft\n\n" +
                "Ez a művelet nem vonható vissza!",
                "Bérlés véglegesítése",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
                return;

            btnFinalize.IsEnabled = false;
            btnFinalize.Content = "💾 Mentés...";

            try
            {
                // 1. Adatbázisba mentés
                await SaveRentalToDatabase();

                // 2. PDF másolása végleges helyre
                // 2. Word fájl másolása végleges helyre
                CopyWordToFinalLocation();

                // 3. Email küldése (opcionális)
                await SendEmailNotification();

                // 4. Sikeres befejezés
                MessageBox.Show(
                    "Bérlés sikeresen véglegesítve!\n\n" +
                    $"Szerződés mentve: {Path.GetFileName(_finalWordPath)}\n" +
                    "Adatok elmentve az adatbázisba.",
                    "Sikeres véglegesítés",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Hiba a bérlés véglegesítése során:\n{ex.Message}",
                    "Hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                btnFinalize.IsEnabled = true;
                btnFinalize.Content = "💾 Bérlés véglegesítése";
            }
        }

        private async Task SaveRentalToDatabase()
        {
            using (var context = new RentAllProContext())
            {
                // Customer mentése vagy frissítése
                var existingCustomer = await context.Customers
                    .FirstOrDefaultAsync(c => c.Email == _customer.Email);

                if (existingCustomer != null)
                {
                    // Meglévő ügyfél frissítése
                    existingCustomer.FullName = _customer.FullName;
                    existingCustomer.PostalCode = _customer.PostalCode;
                    existingCustomer.City = _customer.City;
                    existingCustomer.Address = _customer.Address;
                    existingCustomer.IdNumber = _customer.IdNumber;

                    _customer = existingCustomer;
                    context.Customers.Update(_customer);
                }
                else
                {
                    // Új ügyfél hozzáadása
                    _customer.CreatedAt = DateTime.Now;
                    await context.Customers.AddAsync(_customer);
                }

                await context.SaveChangesAsync();

                // Rental mentése
                _rental.CustomerId = _customer.Id;
                _rental.Customer = _customer;
                _rental.CreatedAt = DateTime.Now;
                _rental.Status = "Active";

                await context.Rentals.AddAsync(_rental);
                await context.SaveChangesAsync();

                // TODO: Ha lesz RentalItems tábla az eszközök kapcsolatához, 
                // akkor itt kell menteni a kiválasztott eszközöket is
            }
        }

        private void CopyWordToFinalLocation()
        {
            if (File.Exists(_tempWordPath))
            {
                File.Copy(_tempWordPath, _finalWordPath, true);

                // Temp fájl törlése
                try
                {
                    File.Delete(_tempWordPath);
                }
                catch
                {
                    // Nem kritikus ha nem sikerül törölni
                }
            }
        }

        private async Task SendEmailNotification()
        {
            try
            {
                var emailService = new EmailService();
                var rentalDetails = $@"
                    <strong>Ügyfél:</strong> {_customer.FullName}<br>
                    <strong>Email:</strong> {_customer.Email}<br>
                    <strong>Bérlési időszak:</strong> {_rental.StartDate:yyyy.MM.dd} - {_rental.ExpectedReturnDate:yyyy.MM.dd}<br>
                    <strong>Bérlési napok:</strong> {_rental.RentalDays}<br>
                    <strong>Fizetési mód:</strong> {_rental.PaymentMethod}<br>
                    <strong>Eszközök száma:</strong> {_selectedEquipments.Count} db<br>
                    <strong>Végösszeg:</strong> {_rental.TotalAmount:N0} Ft<br><br>
                    <strong>Bérelt eszközök:</strong><br>
                    {string.Join("<br>", _selectedEquipments.Select(e => $"• {e.Name} ({e.Code}) - {e.DailyRate:N0} Ft/nap"))}
                ";

                await emailService.SendRentalNotificationAsync(
                    _customer.Email,
                    _customer.FullName,
                    rentalDetails
                );
            }
            catch
            {
                // Email küldési hiba nem akadályozza a bérlés véglegesítését
            }
        }

        #endregion

        #region Navigáció

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Biztosan ki szeretne lépni? A generált PDF és minden adat elvész.",
                "Megerősítés",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                // Temp PDF törlése
                try
                {
                    if (!string.IsNullOrEmpty(_tempWordPath) && File.Exists(_tempWordPath))
                    {
                        File.Delete(_tempWordPath);
                    }
                }
                catch { }

                this.DialogResult = false;
                this.Close();
            }
        }

        #endregion
    }
}
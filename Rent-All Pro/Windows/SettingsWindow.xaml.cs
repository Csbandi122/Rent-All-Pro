using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using RentAllPro.Services;

namespace RentAllPro
{
    public partial class SettingsWindow : Window
    {
        private readonly EmailService _emailService;

        public SettingsWindow()
        {
            InitializeComponent();
            _emailService = new EmailService();
            LoadSettings();
        }

        #region Adatok betöltése és mentése

        private void LoadSettings()
        {
            try
            {
                // Cég adatok betöltése
                txtCompanyName.Text = Properties.Settings.Default.CompanyName;
                txtCompanyWebsite.Text = Properties.Settings.Default.CompanyWebsite;
                txtGoogleReviewUrl.Text = Properties.Settings.Default.GoogleReviewUrl;

                // Template fájlok betöltése
                txtContractTemplatePath.Text = Properties.Settings.Default.ContractTemplatePath;
                txtTermsTemplatePath.Text = Properties.Settings.Default.TermsTemplatePath;

                // Email beállítások betöltése
                txtSmtpServer.Text = Properties.Settings.Default.SmtpServer;
                txtSmtpPort.Text = Properties.Settings.Default.SmtpPort.ToString();
                txtSmtpUsername.Text = Properties.Settings.Default.SmtpUsername;

                // Jelszó betöltése (titkosítva tárolt)
                if (!string.IsNullOrEmpty(Properties.Settings.Default.SmtpPassword))
                {
                    pwdSmtpPassword.Password = DecryptPassword(Properties.Settings.Default.SmtpPassword);
                }

                txtSenderEmail.Text = Properties.Settings.Default.SenderEmail;
                txtSenderName.Text = Properties.Settings.Default.SenderName;
                txtAdditionalRecipients.Text = Properties.Settings.Default.AdditionalRecipients;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Hiba a beállítások betöltése során:\n{ex.Message}",
                    "Hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Cég adatok mentése
                Properties.Settings.Default.CompanyName = txtCompanyName.Text.Trim();
                Properties.Settings.Default.CompanyWebsite = txtCompanyWebsite.Text.Trim();
                Properties.Settings.Default.GoogleReviewUrl = txtGoogleReviewUrl.Text.Trim();

                // Template fájlok mentése
                Properties.Settings.Default.ContractTemplatePath = txtContractTemplatePath.Text.Trim();
                Properties.Settings.Default.TermsTemplatePath = txtTermsTemplatePath.Text.Trim();

                // Email beállítások mentése
                Properties.Settings.Default.SmtpServer = txtSmtpServer.Text.Trim();

                if (int.TryParse(txtSmtpPort.Text, out int port))
                {
                    Properties.Settings.Default.SmtpPort = port;
                }

                Properties.Settings.Default.SmtpUsername = txtSmtpUsername.Text.Trim();

                // Jelszó titkosítva mentése
                if (!string.IsNullOrEmpty(pwdSmtpPassword.Password))
                {
                    Properties.Settings.Default.SmtpPassword = EncryptPassword(pwdSmtpPassword.Password);
                }

                Properties.Settings.Default.SenderEmail = txtSenderEmail.Text.Trim();
                Properties.Settings.Default.SenderName = txtSenderName.Text.Trim();
                Properties.Settings.Default.AdditionalRecipients = txtAdditionalRecipients.Text.Trim();

                // Beállítások mentése
                Properties.Settings.Default.Save();

                MessageBox.Show(
                    "Beállítások sikeresen mentve!",
                    "Siker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                if (this.Owner is MainWindow mainWindow)
                {
                    mainWindow.UpdateHeaderTitle();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Hiba a beállítások mentése során:\n{ex.Message}",
                    "Hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        #endregion

        #region Jelszó titkosítás

        private string EncryptPassword(string password)
        {
            try
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] encrypted = System.Security.Cryptography.ProtectedData.Protect(
                    data, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }
            catch
            {
                return password;
            }
        }

        private string DecryptPassword(string encryptedPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedPassword))
                    return string.Empty;

                byte[] data = Convert.FromBase64String(encryptedPassword);
                byte[] decrypted = System.Security.Cryptography.ProtectedData.Unprotect(
                    data, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return encryptedPassword;
            }
        }


        #endregion

        #region Validáció

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Csak számok engedélyezése
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool ValidateSettings()
        {
            var errors = new List<string>();

            // Email beállítások validálása
            if (string.IsNullOrWhiteSpace(txtSmtpServer.Text))
                errors.Add("• SMTP szerver neve kötelező");

            if (!int.TryParse(txtSmtpPort.Text, out int port) || port <= 0 || port > 65535)
                errors.Add("• Érvényes port számot adjon meg (1-65535)");

            if (string.IsNullOrWhiteSpace(txtSmtpUsername.Text))
                errors.Add("• SMTP felhasználónév kötelező");

            if (string.IsNullOrWhiteSpace(pwdSmtpPassword.Password))
                errors.Add("• SMTP jelszó kötelező");

            if (string.IsNullOrWhiteSpace(txtSenderEmail.Text))
                errors.Add("• Küldő e-mail címe kötelező");
            else if (!IsValidEmail(txtSenderEmail.Text))
                errors.Add("• Érvényes küldő e-mail címet adjon meg");

            if (string.IsNullOrWhiteSpace(txtSenderName.Text))
                errors.Add("• Küldő neve kötelező");

            // További címzettek validálása (ha van)
            if (!string.IsNullOrWhiteSpace(txtAdditionalRecipients.Text))
            {
                var emails = txtAdditionalRecipients.Text.Split(',');
                foreach (var email in emails)
                {
                    if (!IsValidEmail(email.Trim()))
                    {
                        errors.Add($"• Érvénytelen e-mail cím: {email.Trim()}");
                    }
                }
            }

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

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Fájl tallózás

        private void BtnBrowseContract_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Szerződés template kiválasztása",
                Filter = "Word dokumentumok (*.docx)|*.docx|Word dokumentumok (*.doc)|*.doc|Minden fájl (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                txtContractTemplatePath.Text = openFileDialog.FileName;
            }
        }

        private void BtnBrowseTerms_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "ÁSZF template kiválasztása",
                Filter = "Word dokumentumok (*.docx)|*.docx|Word dokumentumok (*.doc)|*.doc|PDF fájlok (*.pdf)|*.pdf|Minden fájl (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                txtTermsTemplatePath.Text = openFileDialog.FileName;
            }
        }

        #endregion

        #region Gomb események

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateSettings())
            {
                SaveSettings();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Biztosan ki szeretne lépni? A nem mentett változtatások elvesznek.",
                "Megerősítés",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        private async void BtnSendTest_Click(object sender, RoutedEventArgs e)
        {
            // Teszt email cím validálása
            if (string.IsNullOrWhiteSpace(txtTestEmail.Text))
            {
                MessageBox.Show(
                    "Kérjük adjon meg egy teszt e-mail címet!",
                    "Hibaüzenet",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                txtTestEmail.Focus();
                return;
            }

            if (!IsValidEmail(txtTestEmail.Text))
            {
                MessageBox.Show(
                    "Kérjük érvényes e-mail címet adjon meg!",
                    "Hibaüzenet",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                txtTestEmail.Focus();
                return;
            }

            // Gomb letiltása a küldés alatt
            btnSendTest.IsEnabled = false;
            btnSendTest.Content = "📧 Küldés...";

            try
            {
                // Beállítások mentése küldés előtt (ha vannak változások)
                if (ValidateSettings())
                {
                    SaveSettings();
                }
                else
                {
                    return; // Ha validációs hiba van, kilépés
                }

                // Email küldése
                var result = await _emailService.SendTestEmailAsync(txtTestEmail.Text.Trim());

                if (result.Success)
                {
                    MessageBox.Show(
                        result.Message + "\n\n" +
                        "✅ Email beállítások működnek!\n" +
                        "💡 Ellenőrizze a spam mappát is.",
                        "Teszt sikeres",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "❌ Email küldési hiba:\n\n" + result.ErrorMessage + "\n\n" +
                        "💡 Ellenőrizze az SMTP beállításokat:\n" +
                        "• SMTP szerver neve\n" +
                        "• Port szám (általában 587 vagy 465)\n" +
                        "• Felhasználónév és jelszó\n" +
                        "• SSL/TLS beállítások",
                        "Email küldési hiba",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Váratlan hiba történt:\n\n{ex.Message}",
                    "Hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                // Gomb visszaállítása
                btnSendTest.IsEnabled = true;
                btnSendTest.Content = "📧 Teszt küldés";
            }
        }

        #endregion
    }
}
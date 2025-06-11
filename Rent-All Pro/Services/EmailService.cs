using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RentAllPro.Services
{
    public class EmailService
    {
        public async Task<EmailResult> SendTestEmailAsync(string testEmailAddress)
        {
            try
            {
                // Beállítások betöltése
                var smtpServer = Properties.Settings.Default.SmtpServer;
                var smtpPort = Properties.Settings.Default.SmtpPort;
                var smtpUsername = Properties.Settings.Default.SmtpUsername;
                var smtpPassword = DecryptPassword(Properties.Settings.Default.SmtpPassword);
                var senderEmail = Properties.Settings.Default.SenderEmail;
                var senderName = Properties.Settings.Default.SenderName;

                // Validáció
                if (string.IsNullOrWhiteSpace(smtpServer) ||
                    string.IsNullOrWhiteSpace(smtpUsername) ||
                    string.IsNullOrWhiteSpace(smtpPassword) ||
                    string.IsNullOrWhiteSpace(senderEmail))
                {
                    return new EmailResult
                    {
                        Success = false,
                        ErrorMessage = "Hiányzó email beállítások! Kérjük töltse ki az összes SMTP mezőt."
                    };
                }

                // SMTP kliens konfigurálása
                using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    // Email üzenet összeállítása
                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(senderEmail, senderName);
                        mailMessage.To.Add(testEmailAddress);
                        mailMessage.Subject = "🧪 Teszt email - Eszközbérlés támogatás";
                        mailMessage.IsBodyHtml = true;

                        mailMessage.Body = CreateTestEmailBody(senderName);

                        // Email küldése
                        await smtpClient.SendMailAsync(mailMessage);
                    }
                }

                return new EmailResult
                {
                    Success = true,
                    Message = $"Teszt email sikeresen elküldve a következő címre: {testEmailAddress}"
                };
            }
            catch (SmtpException smtpEx)
            {
                return new EmailResult
                {
                    Success = false,
                    ErrorMessage = $"SMTP hiba: {smtpEx.Message}\n\nEllenőrizze az SMTP beállításokat!"
                };
            }
            catch (Exception ex)
            {
                return new EmailResult
                {
                    Success = false,
                    ErrorMessage = $"Váratlan hiba: {ex.Message}"
                };
            }
        }

        public async Task<EmailResult> SendRentalNotificationAsync(
            string recipientEmail,
            string customerName,
            string rentalDetails)
        {
            try
            {
                // Beállítások betöltése
                var smtpServer = Properties.Settings.Default.SmtpServer;
                var smtpPort = Properties.Settings.Default.SmtpPort;
                var smtpUsername = Properties.Settings.Default.SmtpUsername;
                var smtpPassword = DecryptPassword(Properties.Settings.Default.SmtpPassword);
                var senderEmail = Properties.Settings.Default.SenderEmail;
                var senderName = Properties.Settings.Default.SenderName;
                var additionalRecipients = Properties.Settings.Default.AdditionalRecipients;

                using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(senderEmail, senderName);
                        mailMessage.To.Add(recipientEmail);

                        // További címzettek hozzáadása
                        if (!string.IsNullOrWhiteSpace(additionalRecipients))
                        {
                            var emails = additionalRecipients.Split(',');
                            foreach (var email in emails)
                            {
                                var trimmedEmail = email.Trim();
                                if (!string.IsNullOrWhiteSpace(trimmedEmail))
                                {
                                    mailMessage.CC.Add(trimmedEmail);
                                }
                            }
                        }

                        mailMessage.Subject = $"📋 Új bérlés - {customerName}";
                        mailMessage.IsBodyHtml = true;
                        mailMessage.Body = CreateRentalNotificationBody(customerName, rentalDetails);

                        await smtpClient.SendMailAsync(mailMessage);
                    }
                }

                return new EmailResult { Success = true };
            }
            catch (Exception ex)
            {
                return new EmailResult
                {
                    Success = false,
                    ErrorMessage = $"Email küldési hiba: {ex.Message}"
                };
            }
        }

        private string CreateTestEmailBody(string senderName)
        {
            var companyName = Properties.Settings.Default.CompanyName;
            if (string.IsNullOrWhiteSpace(companyName))
                companyName = "Eszközbérlés vállalkozás";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #2E86AB; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #666; }}
        .success {{ background-color: #d4edda; border: 1px solid #c3e6cb; color: #155724; padding: 15px; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🧪 Teszt Email</h1>
        <h2>{companyName}</h2>
    </div>
    
    <div class='content'>
        <div class='success'>
            <strong>✅ Siker!</strong> Az email beállítások megfelelően működnek.
        </div>
        
        <h3>📧 Email rendszer információk:</h3>
        <ul>
            <li><strong>Küldő neve:</strong> {senderName}</li>
            <li><strong>Teszt időpontja:</strong> {DateTime.Now:yyyy.MM.dd HH:mm:ss}</li>
            <li><strong>Rendszer:</strong> Rent-All-Pro Eszközbérlés támogatás</li>
        </ul>
        
        <p>Ez egy automatikusan generált teszt üzenet. Ha megkapta, az email beállítások helyesen vannak konfigurálva.</p>
        
        <hr>
        <p><strong>Következő lépések:</strong></p>
        <ol>
            <li>Ellenőrizze a spam mappát is</li>
            <li>Tesztelje a bérlési értesítőket is</li>
            <li>Állítsa be a további címzetteket, ha szükséges</li>
        </ol>
    </div>
    
    <div class='footer'>
        <p>Rent-All-Pro Eszközbérlés támogatás | Email teszt üzenet</p>
        <p>Generálva: {DateTime.Now:yyyy.MM.dd HH:mm:ss}</p>
    </div>
</body>
</html>";
        }

        private string CreateRentalNotificationBody(string customerName, string rentalDetails)
        {
            var companyName = Properties.Settings.Default.CompanyName;
            if (string.IsNullOrWhiteSpace(companyName))
                companyName = "Eszközbérlés vállalkozás";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #2E86AB; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #666; }}
        .rental-info {{ background-color: #f8f9fa; border-left: 4px solid #2E86AB; padding: 15px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>📋 Új bérlés értesítő</h1>
        <h2>{companyName}</h2>
    </div>
    
    <div class='content'>
        <h3>Új bérlés érkezett: <strong>{customerName}</strong></h3>
        
        <div class='rental-info'>
            <h4>📋 Bérlési részletek:</h4>
            {rentalDetails}
        </div>
        
        <p><strong>Időpont:</strong> {DateTime.Now:yyyy.MM.dd HH:mm:ss}</p>
        
        <hr>
        <p><em>Ez egy automatikusan generált értesítő a Rent-All-Pro rendszerből.</em></p>
    </div>
    
    <div class='footer'>
        <p>{companyName} | Bérlési értesítő</p>
        <p>Generálva: {DateTime.Now:yyyy.MM.dd HH:mm:ss}</p>
    </div>
</body>
</html>";
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

        public class EmailResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string ErrorMessage { get; set; } = string.Empty;
        }
    }
}
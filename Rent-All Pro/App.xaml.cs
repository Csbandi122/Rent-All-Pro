using RentAllPro.Data;
using System;
using System.Windows;

namespace RentAllPro
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Adatbázis inicializálása
            try
            {
                using (var context = new RentAllProContext())
                {
                    context.Database.EnsureCreated();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Hiba az adatbázis inicializálása során:\n{ex.Message}",
                    "Adatbázis hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}
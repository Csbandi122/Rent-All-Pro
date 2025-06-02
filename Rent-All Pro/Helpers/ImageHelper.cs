using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace RentAllPro.Helpers
{
    public static class ImageHelper
    {
        private const int TARGET_HEIGHT = 150;
        private const int MAX_WIDTH = 200;
        private static readonly string PHOTOS_FOLDER = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos", "Equipment");

        public static string SaveEquipmentImage(string sourceImagePath, string equipmentCode)
        {
            try
            {
                // Photos/Equipment mappa létrehozása ha nem létezik
                Directory.CreateDirectory(PHOTOS_FOLDER);

                // Új fájlnév generálása
                string extension = Path.GetExtension(sourceImagePath).ToLower();
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string newFileName = $"{equipmentCode}_{timestamp}{extension}";
                string destinationPath = Path.Combine(PHOTOS_FOLDER, newFileName);

                // Kép betöltése és átméretezése
                using (var originalImage = new Bitmap(sourceImagePath))
                {
                    var resizedImage = ResizeImage(originalImage, TARGET_HEIGHT, MAX_WIDTH);

                    // Kép mentése
                    ImageFormat format = GetImageFormat(extension);
                    resizedImage.Save(destinationPath, format);
                    resizedImage.Dispose();
                }

                // Relatív elérési út visszaadása
                return Path.Combine("Photos", "Equipment", newFileName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Hiba a kép mentése során: {ex.Message}");
            }
        }

        private static Bitmap ResizeImage(Bitmap originalImage, int targetHeight, int maxWidth)
        {
            // Arányok számítása
            float ratio = (float)targetHeight / originalImage.Height;
            int newWidth = (int)(originalImage.Width * ratio);

            // Ha túl széles lenne, korlátozás a max szélességre
            if (newWidth > maxWidth)
            {
                ratio = (float)maxWidth / originalImage.Width;
                newWidth = maxWidth;
                targetHeight = (int)(originalImage.Height * ratio);
            }

            // Átméretezett kép létrehozása
            var resizedImage = new Bitmap(newWidth, targetHeight);
            using (var graphics = Graphics.FromImage(resizedImage))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(originalImage, 0, 0, newWidth, targetHeight);
            }

            return resizedImage;
        }

        private static ImageFormat GetImageFormat(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                ".png" => ImageFormat.Png,
                ".bmp" => ImageFormat.Bmp,
                ".gif" => ImageFormat.Gif,
                _ => ImageFormat.Jpeg
            };
        }

        public static BitmapImage LoadImageForDisplay(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                    return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(Path.GetFullPath(imagePath));
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public static void DeleteEquipmentImage(string imagePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
            }
            catch
            {
                // Némán elnyeljük a hibát, nem kritikus
            }
        }
    }
}
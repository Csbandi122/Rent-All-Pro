using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Word = DocumentFormat.OpenXml.Wordprocessing;
using RentAllPro.Models;

namespace RentAllPro.Services
{
    public class PdfService
    {
        public async Task<PdfResult> GenerateContractPdfAsync(
            Customer customer,
            Rental rental,
            List<Equipment> selectedEquipments,
            string outputPath)
        {
            return await Task.Run(() => GenerateContractWord(customer, rental, selectedEquipments, outputPath));
        }

        private PdfResult GenerateContractWord(
            Customer customer,
            Rental rental,
            List<Equipment> selectedEquipments,
            string outputPath)
        {
            try
            {
                // 1. Template elérési út ellenőrzése
                var templatePath = Properties.Settings.Default.ContractTemplatePath;
                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                {
                    return new PdfResult
                    {
                        Success = false,
                        ErrorMessage = "Szerződés template nincs beállítva vagy nem található!\n\n" +
                                       "Kérjük állítsa be a template elérési útját a Beállítások menüben."
                    };
                }

                // 2. Word fájl létrehozása (PDF helyett)
                var wordPath = Path.ChangeExtension(outputPath, ".docx");

                // 3. Template másolása
                File.Copy(templatePath, wordPath, true);

                // 4. Word dokumentum módosítása
                using (var document = WordprocessingDocument.Open(wordPath, true))
                {
                    // Változók helyettesítése
                    ReplaceVariables(document, customer, rental, selectedEquipments);

                    // Táblázat beszúrása
                    InsertEquipmentTable(document, selectedEquipments, rental.RentalDays);
                }

                return new PdfResult
                {
                    Success = true,
                    WordFilePath = wordPath  // Word fájl elérési útja
                };
            }
            catch (Exception ex)
            {
                return new PdfResult
                {
                    Success = false,
                    ErrorMessage = $"Hiba a szerződés generálás során:\n{ex.Message}"
                };
            }
        }

        private void ReplaceVariables(WordprocessingDocument document, Customer customer, Rental rental, List<Equipment> selectedEquipments)
        {
            var replacements = new Dictionary<string, string>
            {
                { "{name}", customer.FullName },
                { "{address}", $"{customer.PostalCode} {customer.City}, {customer.Address}" },
                { "{email}", customer.Email },
                { "{idnumber}", customer.IdNumber },
                { "{rentstart}", rental.StartDate.ToString("yyyy.MM.dd") },
                { "{rentaldays}", rental.RentalDays.ToString() },
                { "{nrofbikes}", selectedEquipments.Count.ToString() },
                { "{totalprice}", rental.TotalAmount.ToString("N0") }
            };

            // Minden text elemben végigmegyünk
            var textElements = document.MainDocumentPart.Document.Descendants<Word.Text>();

            foreach (var textElement in textElements)
            {
                foreach (var replacement in replacements)
                {
                    if (textElement.Text.Contains(replacement.Key))
                    {
                        textElement.Text = textElement.Text.Replace(replacement.Key, replacement.Value);
                    }
                }
            }
        }

        private void InsertEquipmentTable(WordprocessingDocument document, List<Equipment> selectedEquipments, int rentalDays)
        {
            try
            {
                // {tablehere} placeholder megkeresése
                var textElements = document.MainDocumentPart.Document.Descendants<Word.Text>();
                Word.Text tableplaceholder = null;

                foreach (var textElement in textElements)
                {
                    if (textElement.Text.Contains("{tablehere}"))
                    {
                        tableplaceholder = textElement;
                        break;
                    }
                }

                if (tableplaceholder != null)
                {
                    // Táblázat létrehozása
                    var table = CreateEquipmentTable(selectedEquipments, rentalDays);

                    // Placeholder eltávolítása és táblázat beszúrása
                    var paragraph = tableplaceholder.Ancestors<Word.Paragraph>().FirstOrDefault();
                    if (paragraph != null)
                    {
                        tableplaceholder.Text = tableplaceholder.Text.Replace("{tablehere}", "");

                        // Táblázat beszúrása a paragraph után
                        paragraph.Parent.InsertAfter(table, paragraph);
                    }
                }
                else
                {
                    // Ha nincs placeholder, dokumentum végére tesszük
                    var body = document.MainDocumentPart.Document.Body;
                    var table = CreateEquipmentTable(selectedEquipments, rentalDays);
                    body.AppendChild(table);
                }
            }
            catch (Exception ex)
            {
                // Ha táblázat nem sikerül, szöveges lista
                CreateEquipmentTextList(document, selectedEquipments, rentalDays);
            }
        }

        private Word.Table CreateEquipmentTable(List<Equipment> selectedEquipments, int rentalDays)
        {
            var table = new Word.Table();

            // Táblázat tulajdonságai
            var tableProperties = new Word.TableProperties(
                new Word.TableBorders(
                    new Word.TopBorder() { Val = new EnumValue<Word.BorderValues>(Word.BorderValues.Single), Size = 12 },
                    new Word.BottomBorder() { Val = new EnumValue<Word.BorderValues>(Word.BorderValues.Single), Size = 12 },
                    new Word.LeftBorder() { Val = new EnumValue<Word.BorderValues>(Word.BorderValues.Single), Size = 12 },
                    new Word.RightBorder() { Val = new EnumValue<Word.BorderValues>(Word.BorderValues.Single), Size = 12 },
                    new Word.InsideHorizontalBorder() { Val = new EnumValue<Word.BorderValues>(Word.BorderValues.Single), Size = 12 },
                    new Word.InsideVerticalBorder() { Val = new EnumValue<Word.BorderValues>(Word.BorderValues.Single), Size = 12 }
                )
            );
            table.AppendChild(tableProperties);

            // Fejléc sor
            var headerRow = new Word.TableRow();
            headerRow.AppendChild(CreateTableCell("Eszköz kódja", true));
            headerRow.AppendChild(CreateTableCell("Eszköz neve és típusa", true));
            headerRow.AppendChild(CreateTableCell("Napi bérleti díj", true));
            headerRow.AppendChild(CreateTableCell("Összesen", true));
            table.AppendChild(headerRow);

            // Eszköz sorok
            foreach (var equipment in selectedEquipments)
            {
                var row = new Word.TableRow();
                row.AppendChild(CreateTableCell(equipment.Code));
                row.AppendChild(CreateTableCell($"{equipment.Name} ({equipment.Type})"));
                row.AppendChild(CreateTableCell($"{equipment.DailyRate:N0} Ft"));
                row.AppendChild(CreateTableCell($"{equipment.DailyRate * rentalDays:N0} Ft"));
                table.AppendChild(row);
            }

            return table;
        }

        private Word.TableCell CreateTableCell(string text, bool isHeader = false)
        {
            var cell = new Word.TableCell();

            var paragraph = new Word.Paragraph();
            var run = new Word.Run();
            var runText = new Word.Text(text);

            if (isHeader)
            {
                var runProperties = new Word.RunProperties();
                runProperties.AppendChild(new Word.Bold());
                run.AppendChild(runProperties);
            }

            run.AppendChild(runText);
            paragraph.AppendChild(run);
            cell.AppendChild(paragraph);

            return cell;
        }

        private void CreateEquipmentTextList(WordprocessingDocument document, List<Equipment> selectedEquipments, int rentalDays)
        {
            var body = document.MainDocumentPart.Document.Body;

            // Címsor
            var titleParagraph = new Word.Paragraph();
            var titleRun = new Word.Run();
            titleRun.AppendChild(new Word.Text("Bérelt eszközök részletei:"));
            titleParagraph.AppendChild(titleRun);
            body.AppendChild(titleParagraph);

            // Eszközök listája
            foreach (var equipment in selectedEquipments)
            {
                var paragraph = new Word.Paragraph();
                var run = new Word.Run();
                var text = $"• {equipment.Name} ({equipment.Code}) - {equipment.DailyRate:N0} Ft/nap, összesen: {equipment.DailyRate * rentalDays:N0} Ft";
                run.AppendChild(new Word.Text(text));
                paragraph.AppendChild(run);
                body.AppendChild(paragraph);
            }
        }
    }

    public class PdfResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string WordFilePath { get; set; } = string.Empty;  // Word fájl elérési útja
    }
}
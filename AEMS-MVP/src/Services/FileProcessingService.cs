using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace AEMSApp.Services
{
    public class FileProcessingService
    {
        public string ExtractTextFromPdf(string filePath)
        {
            try
            {
                StringBuilder text = new StringBuilder();
                using (PdfReader pdfReader = new PdfReader(filePath))
                using (PdfDocument pdfDoc = new PdfDocument(pdfReader))
                {
                    for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                    {
                        var strategy = new LocationTextExtractionStrategy();
                        var pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
                        text.AppendLine(pageText);
                    }
                }
                return text.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting text from PDF: {ex.Message}", ex);
            }
        }

        public string ExtractTextFromDocx(string filePath)
        {
            try
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
                {
                    var body = doc.MainDocumentPart?.Document.Body;
                    if (body != null)
                    {
                        return body.InnerText;
                    }
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting text from DOCX: {ex.Message}", ex);
            }
        }

        public string ExtractTextFromPptx(string filePath)
        {
            try
            {
                StringBuilder text = new StringBuilder();
                using (PresentationDocument ppt = PresentationDocument.Open(filePath, false))
                {
                    var presentation = ppt.PresentationPart?.Presentation;
                    if (presentation != null)
                    {
                        foreach (var slidePart in ppt.PresentationPart.SlideParts)
                        {
                            var slide = slidePart.Slide;
                            text.AppendLine(slide.InnerText);
                        }
                    }
                    return text.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting text from PPTX: {ex.Message}", ex);
            }
        }

        public string ExtractTextFromXlsx(string filePath)
        {
            try
            {
                StringBuilder text = new StringBuilder();
                using (SpreadsheetDocument xlsx = SpreadsheetDocument.Open(filePath, false))
                {
                    var workbookPart = xlsx.WorkbookPart;
                    if (workbookPart != null)
                    {
                        foreach (var sheet in workbookPart.Workbook.Sheets.Elements<Sheet>())
                        {
                            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
                            var cells = worksheetPart.Worksheet.Descendants<Cell>();
                            foreach (var cell in cells)
                            {
                                if (cell.CellValue != null)
                                {
                                    text.AppendLine(cell.CellValue.Text);
                                }
                            }
                        }
                    }
                    return text.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting text from XLSX: {ex.Message}", ex);
            }
        }

        public string GetMediaFileContext(string filePath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var directory = Path.GetDirectoryName(filePath);
                var parentFolder = Path.GetFileName(directory);

                var context = new StringBuilder();
                context.AppendLine($"File Name: {fileName}");
                context.AppendLine($"Parent Folder: {parentFolder}");
                
                // Get creation and modification dates
                var fileInfo = new FileInfo(filePath);
                context.AppendLine($"Created: {fileInfo.CreationTime}");
                context.AppendLine($"Modified: {fileInfo.LastWriteTime}");

                return context.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting media file context: {ex.Message}", ex);
            }
        }
    }
}

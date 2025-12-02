using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace WinterAdventurer.Test.Helpers
{
    /// <summary>
    /// Helper class for PDF testing. Provides utilities for rendering MigraDoc documents
    /// to PDF bytes and extracting/validating PDF content using PdfPig.
    /// </summary>
    public static class PdfTestHelper
    {
        /// <summary>
        /// Renders a MigraDoc Document to PDF byte array.
        /// </summary>
        /// <param name="document">The MigraDoc document to render. Can be null (returns empty array).</param>
        /// <returns>PDF file as byte array, or empty array if document is null.</returns>
        public static byte[] RenderPdfToBytes(Document? document)
        {
            if (document == null)
            {
                return Array.Empty<byte>();
            }

            using var stream = new MemoryStream();
            var renderer = new PdfDocumentRenderer
            {
                Document = document
            };
            renderer.RenderDocument();
            renderer.PdfDocument.Save(stream, false);
            return stream.ToArray();
        }

        /// <summary>
        /// Extracts all text from all pages of a PDF.
        /// </summary>
        /// <param name="pdfBytes">PDF file as byte array.</param>
        /// <returns>All text content concatenated from all pages, separated by newlines.</returns>
        public static string ExtractAllText(byte[] pdfBytes)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return string.Empty;
            }

            using var document = PdfDocument.Open(pdfBytes);
            var allText = new System.Text.StringBuilder();

            foreach (Page page in document.GetPages())
            {
                allText.AppendLine(page.Text);
            }

            return allText.ToString();
        }

        /// <summary>
        /// Extracts text from a specific page of a PDF.
        /// </summary>
        /// <param name="pdfBytes">PDF file as byte array.</param>
        /// <param name="pageNumber">Page number (1-indexed).</param>
        /// <returns>Text content from the specified page.</returns>
        public static string ExtractPageText(byte[] pdfBytes, int pageNumber)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return string.Empty;
            }

            using var document = PdfDocument.Open(pdfBytes);

            if (pageNumber < 1 || pageNumber > document.NumberOfPages)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber),
                    $"Page number {pageNumber} is out of range. PDF has {document.NumberOfPages} pages.");
            }

            Page page = document.GetPage(pageNumber);
            return page.Text;
        }

        /// <summary>
        /// Asserts that a PDF contains specific text content.
        /// </summary>
        /// <param name="pdfBytes">PDF file as byte array.</param>
        /// <param name="expectedText">Text that should appear in the PDF.</param>
        /// <param name="context">Context for the assertion message (e.g., "Workshop roster", "Schedule").</param>
        public static void AssertContainsText(byte[] pdfBytes, string expectedText, string context = "PDF")
        {
            string allText = ExtractAllText(pdfBytes);

            // PDF text extraction may not preserve spaces consistently
            // So we normalize both strings by removing whitespace for comparison
            string normalizedExpected = expectedText.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
            string normalizedActual = allText.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");

            if (!normalizedActual.Contains(normalizedExpected, StringComparison.OrdinalIgnoreCase))
            {
                throw new AssertFailedException(
                    $"{context} should contain '{expectedText}'. " +
                    $"Actual text length: {allText.Length} characters. " +
                    $"First 200 chars: {(allText.Length > 200 ? allText.Substring(0, 200) : allText)}");
            }
        }

        /// <summary>
        /// Counts the number of images on a specific page of a PDF.
        /// </summary>
        /// <param name="pdfBytes">PDF file as byte array.</param>
        /// <param name="pageNumber">Page number (1-indexed). Defaults to page 1.</param>
        /// <returns>Number of images found on the specified page.</returns>
        public static int CountImages(byte[] pdfBytes, int pageNumber = 1)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return 0;
            }

            using var document = PdfDocument.Open(pdfBytes);

            if (pageNumber < 1 || pageNumber > document.NumberOfPages)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber),
                    $"Page number {pageNumber} is out of range. PDF has {document.NumberOfPages} pages.");
            }

            Page page = document.GetPage(pageNumber);
            return page.GetImages().Count();
        }

        /// <summary>
        /// Asserts that a PDF page contains a specific number of images.
        /// </summary>
        /// <param name="pdfBytes">PDF file as byte array.</param>
        /// <param name="expectedCount">Expected number of images.</param>
        /// <param name="pageNumber">Page number (1-indexed). Defaults to page 1.</param>
        public static void AssertImageCount(byte[] pdfBytes, int expectedCount, int pageNumber = 1)
        {
            int actualCount = CountImages(pdfBytes, pageNumber);

            if (actualCount != expectedCount)
            {
                throw new AssertFailedException(
                    $"Expected {expectedCount} image(s) on page {pageNumber}, but found {actualCount}.");
            }
        }

        /// <summary>
        /// Extracts individual words from a specific page of a PDF.
        /// Useful for precise text verification.
        /// </summary>
        /// <param name="pdfBytes">PDF file as byte array.</param>
        /// <param name="pageNumber">Page number (1-indexed). Defaults to page 1.</param>
        /// <returns>List of words extracted from the page.</returns>
        public static List<string> ExtractWords(byte[] pdfBytes, int pageNumber = 1)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return new List<string>();
            }

            using var document = PdfDocument.Open(pdfBytes);

            if (pageNumber < 1 || pageNumber > document.NumberOfPages)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber),
                    $"Page number {pageNumber} is out of range. PDF has {document.NumberOfPages} pages.");
            }

            Page page = document.GetPage(pageNumber);
            return page.GetWords().Select(w => w.Text).ToList();
        }

        /// <summary>
        /// Gets the total number of pages in a PDF.
        /// </summary>
        /// <param name="pdfBytes">PDF file as byte array.</param>
        /// <returns>Number of pages in the PDF.</returns>
        public static int GetPageCount(byte[] pdfBytes)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return 0;
            }

            using var document = PdfDocument.Open(pdfBytes);
            return document.NumberOfPages;
        }

        /// <summary>
        /// Asserts that a PDF has a specific number of pages.
        /// </summary>
        /// <param name="pdfBytes">PDF file as byte array.</param>
        /// <param name="expectedPageCount">Expected number of pages.</param>
        public static void AssertPageCount(byte[] pdfBytes, int expectedPageCount)
        {
            int actualCount = GetPageCount(pdfBytes);

            if (actualCount != expectedPageCount)
            {
                throw new AssertFailedException(
                    $"Expected {expectedPageCount} page(s), but PDF has {actualCount} page(s).");
            }
        }
    }
}

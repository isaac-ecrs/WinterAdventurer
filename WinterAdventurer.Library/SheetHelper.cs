// <copyright file="SheetHelper.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using OfficeOpenXml;

namespace WinterAdventurer.Library
{
    public class SheetHelper
    {
        private Dictionary<string, int> _columnMap = new Dictionary<string, int>();
        private ExcelWorksheet _sheet;

        /// <summary>
        /// Initializes a new instance of the <see cref="SheetHelper"/> class.
        /// Initializes SheetHelper by building a map of column headers to column indexes.
        /// This enables efficient column lookups by name or pattern throughout Excel parsing.
        /// </summary>
        /// <param name="sheet">Excel worksheet to wrap with helper functionality.</param>
        public SheetHelper(ExcelWorksheet sheet)
        {
            _sheet = sheet;
            if (sheet?.Dimension == null)
            {
                return;
            }

            // Build a map of column header names to column indexes
            for (int col = 1; col <= sheet.Dimension.Columns; col++)
            {
                var headerValue = sheet.Cells[1, col].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(headerValue))
                {
                    _columnMap[headerValue] = col;
                }
            }
        }

        /// <summary>
        /// Gets the column index for an exact header name match.
        /// Used when event schema specifies exact column names.
        /// </summary>
        /// <param name="headerName">Exact header name to find (case-sensitive).</param>
        /// <returns>1-based column index, or null if header not found.</returns>
        public int? GetColumnIndex(string headerName)
        {
            return _columnMap.TryGetValue(headerName, out int col) ? col : null;
        }

        /// <summary>
        /// Gets the column index for the first header containing the pattern substring.
        /// Used when event schema specifies pattern-based column matching (e.g., "WinterAdventureClassRegist_Id" matches "2024WinterAdventureClassRegist_Id").
        /// </summary>
        /// <param name="pattern">Substring pattern to search for in column headers.</param>
        /// <returns>1-based column index of first matching header, or null if no match found.</returns>
        public int? GetColumnIndexByPattern(string pattern)
        {
            // Find first column that contains the pattern
            var matchingKey = _columnMap.Keys.FirstOrDefault(k => k.Contains(pattern));
            return matchingKey != null ? _columnMap[matchingKey] : null;
        }

        /// <summary>
        /// Gets cell value at specified row using exact header name match.
        /// Combines column lookup and value retrieval for cleaner code.
        /// </summary>
        /// <param name="row">1-based row number to read from.</param>
        /// <param name="headerName">Exact column header name.</param>
        /// <returns>Cell value as string, or null if column not found or cell is empty.</returns>
        public string? GetCellValue(int row, string headerName)
        {
            var colIndex = GetColumnIndex(headerName);
            if (!colIndex.HasValue)
            {
                return null;
            }

            return _sheet.Cells[row, colIndex.Value].Value?.ToString();
        }

        /// <summary>
        /// Gets cell value at specified row using pattern-based header matching.
        /// Enables schema-driven Excel parsing where column names vary by year or event.
        /// </summary>
        /// <param name="row">1-based row number to read from.</param>
        /// <param name="pattern">Substring pattern to match against column headers.</param>
        /// <returns>Cell value as string, or null if no matching column found or cell is empty.</returns>
        public string? GetCellValueByPattern(int row, string pattern)
        {
            var colIndex = GetColumnIndexByPattern(pattern);
            if (!colIndex.HasValue)
            {
                return null;
            }

            return _sheet.Cells[row, colIndex.Value].Value?.ToString();
        }

        /// <summary>
        /// Gets cell value at specified row and column index directly.
        /// Used when column position is already known or determined programmatically.
        /// </summary>
        /// <param name="row">1-based row number to read from.</param>
        /// <param name="col">1-based column number to read from.</param>
        /// <returns>Cell value as string, or null if cell is empty.</returns>
        public string? GetCellValueByIndex(int row, int col)
        {
            return _sheet.Cells[row, col].Value?.ToString();
        }
    }
}

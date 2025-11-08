using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;

namespace WinterAdventurer.Library
{
    public class SheetHelper
    {
        private Dictionary<string, int> _columnMap = new Dictionary<string, int>();
        private ExcelWorksheet _sheet;

        public SheetHelper(ExcelWorksheet sheet)
        {
            _sheet = sheet;
            if (sheet?.Dimension == null) return;

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

        public int? GetColumnIndex(string headerName)
        {
            return _columnMap.TryGetValue(headerName, out int col) ? col : null;
        }

        public int? GetColumnIndexByPattern(string pattern)
        {
            // Find first column that contains the pattern
            var matchingKey = _columnMap.Keys.FirstOrDefault(k => k.Contains(pattern));
            return matchingKey != null ? _columnMap[matchingKey] : null;
        }

        public string? GetCellValue(int row, string headerName)
        {
            var colIndex = GetColumnIndex(headerName);
            if (!colIndex.HasValue) return null;

            return _sheet.Cells[row, colIndex.Value].Value?.ToString();
        }

        public string? GetCellValueByPattern(int row, string pattern)
        {
            var colIndex = GetColumnIndexByPattern(pattern);
            if (!colIndex.HasValue) return null;

            return _sheet.Cells[row, colIndex.Value].Value?.ToString();
        }

        public string? GetCellValueByIndex(int row, int col)
        {
            return _sheet.Cells[row, col].Value?.ToString();
        }
    }
}

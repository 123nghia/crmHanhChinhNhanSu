using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text;
using System.Text.RegularExpressions;

namespace VS.Human.Business.Helpers
{
    public static class ExcelHelper
    {
        public static IEnumerable<string> GetRowValues(Row row, WorkbookPart workbookPart)
        {
            var sharedStringTable = workbookPart?.SharedStringTablePart?.SharedStringTable;
            var values = new List<string>();
            int currentIndex = 0;

            foreach (var cell in row.Elements<Cell>())
            {
                var cellRef = cell.CellReference?.Value;
                int cellIndex = currentIndex;

                if (!string.IsNullOrEmpty(cellRef))
                {
                    cellIndex = GetColumnIndex(cellRef);
                }

                // Fill gaps with empty strings
                while (currentIndex < cellIndex)
                {
                    values.Add(string.Empty);
                    currentIndex++;
                }

                var value = GetCellValue(cell, sharedStringTable);
                values.Add(value ?? string.Empty);
                currentIndex++;
            }

            return values;
        }

        public static string? GetCellValue(Cell cell, SharedStringTable? sharedStringTable)
        {
            if (cell.DataType != null && cell.DataType.Value == CellValues.InlineString && cell.InlineString != null)
            {
                if (cell.InlineString.Text != null)
                {
                    return cell.InlineString.Text.Text;
                }

                var inlineBuilder = new StringBuilder();
                foreach (var element in cell.InlineString.Elements())
                {
                    if (element is Text inlineText)
                    {
                        inlineBuilder.Append(inlineText.Text);
                    }
                    else if (element is Run inlineRun)
                    {
                        if (inlineRun.Text != null)
                        {
                            inlineBuilder.Append(inlineRun.Text.Text);
                        }
                    }
                }
                return inlineBuilder.ToString();
            }

            if (cell.CellValue == null)
                return null;

            var value = cell.CellValue.Text;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                if (int.TryParse(value, out int index) && sharedStringTable != null)
                {
                    var item = sharedStringTable.Elements<SharedStringItem>().ElementAtOrDefault(index);
                    if (item == null) return null;

                    var sb = new StringBuilder();
                    foreach (var element in item.Elements())
                    {
                        if (element is Text text)
                        {
                            sb.Append(text.Text);
                        }
                        else if (element is Run run)
                        {
                            if (run.Text != null)
                                sb.Append(run.Text.Text);
                        }
                    }
                    return sb.ToString();
                }
            }

            return value;
        }

        public static int GetColumnIndex(string cellReference)
        {
            string columnName = Regex.Replace(cellReference, "[0-9]", "");
            int columnIndex = 0;
            int factor = 1;

            for (int i = columnName.Length - 1; i >= 0; i--)
            {
                if (char.IsLetter(columnName[i]))
                {
                    columnIndex += (char.ToUpper(columnName[i]) - 'A' + 1) * factor;
                    factor *= 26;
                }
            }

            return columnIndex - 1;
        }

        public static bool TryParseDate(string dateStr, out DateTime date)
        {
            date = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(dateStr))
                return false;

            if (double.TryParse(dateStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var oaDate))
            {
                try
                {
                    date = DateTime.FromOADate(oaDate);
                    return true;
                }
                catch (ArgumentException)
                {
                    // fall back to string parsing
                }
            }

            var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "MM/yyyy", "M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "MM/dd/yyyy" };
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateStr, format, null, System.Globalization.DateTimeStyles.None, out date))
                {
                    return true;
                }
            }

            return DateTime.TryParse(dateStr, out date);
        }
    }
}

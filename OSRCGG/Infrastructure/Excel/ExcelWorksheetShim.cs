using System;
using System.Globalization;
using ClosedXML.Excel;

namespace OSRCGG
{
    internal sealed class ExcelWorksheetShim
    {
        public ExcelWorksheetShim(IXLWorksheet worksheet)
        {
            Worksheet = worksheet;
            Cells = new ExcelCellAccessor(worksheet);
        }

        public IXLWorksheet Worksheet { get; private set; }
        public ExcelCellAccessor Cells { get; private set; }

        public static void WriteClosedXmlValue(IXLCell cell, object value)
        {
            if (cell == null) return;
            if (value == null)
            {
                cell.Clear();
                return;
            }

            if (value is string stringValue) cell.SetValue(stringValue);
            else if (value is int intValue) cell.SetValue(intValue);
            else if (value is long longValue) cell.SetValue(longValue);
            else if (value is double doubleValue) cell.SetValue(doubleValue);
            else if (value is float floatValue) cell.SetValue((double)floatValue);
            else if (value is decimal decimalValue) cell.SetValue((double)decimalValue);
            else if (value is bool boolValue) cell.SetValue(boolValue);
            else if (value is DateTime dateValue) cell.SetValue(dateValue);
            else cell.SetValue(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "");
        }

        // Workbook-сервисы должны одинаково читать пустые ячейки, числа с запятой
        // и используемые диапазоны; поэтому низкоуровневые Excel-операции собраны здесь.
        public static void WriteHeaderRow(IXLWorksheet sheet, string[] headers)
        {
            if (sheet == null || headers == null) return;
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cell(1, i + 1).SetValue(headers[i]);
            }
        }

        public static void WriteHeaderRow(ExcelWorksheetShim sheet, string[] headers)
        {
            if (sheet == null) return;
            WriteHeaderRow(sheet.Worksheet, headers);
        }

        public static void SetValues(ExcelWorksheetShim sheet, object[,] values)
        {
            if (sheet == null || sheet.Worksheet == null || values == null) return;
            int rows = values.GetLength(0);
            int columns = values.GetLength(1);

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    WriteClosedXmlValue(sheet.Worksheet.Cell(row + 1, column + 1), values[row, column]);
                }
            }
        }

        public static IXLWorksheet FindWorksheet(XLWorkbook workbook, string name)
        {
            if (workbook == null) return null;
            foreach (IXLWorksheet sheet in workbook.Worksheets)
            {
                if (string.Equals(sheet.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return sheet;
                }
            }

            return null;
        }

        public static int GetUsedRows(IXLWorksheet sheet)
        {
            return GetUsedRows(new ExcelWorksheetShim(sheet));
        }

        public static int GetUsedRows(ExcelWorksheetShim sheet)
        {
            if (sheet == null || sheet.Worksheet == null) return 0;
            IXLRange range = sheet.Worksheet.RangeUsed();
            return range == null ? 0 : range.LastRow().RowNumber();
        }

        public static int GetUsedColumns(IXLWorksheet sheet)
        {
            return GetUsedColumns(new ExcelWorksheetShim(sheet));
        }

        public static int GetUsedColumns(ExcelWorksheetShim sheet)
        {
            if (sheet == null || sheet.Worksheet == null) return 0;
            IXLRange range = sheet.Worksheet.RangeUsed();
            return range == null ? 0 : range.LastColumn().ColumnNumber();
        }

        public static int GetHeaderColumn(IXLWorksheet sheet, string header, int fallback)
        {
            if (sheet == null || string.IsNullOrWhiteSpace(header)) return fallback;
            int columns = GetUsedColumns(sheet);
            for (int column = 1; column <= columns; column++)
            {
                if (string.Equals(ReadString(sheet, 1, column), header, StringComparison.OrdinalIgnoreCase))
                {
                    return column;
                }
            }

            return fallback;
        }

        public static object[,] GetUsedRangeValues(ExcelWorksheetShim sheet)
        {
            if (sheet == null || sheet.Worksheet == null) return null;
            IXLRange range = sheet.Worksheet.RangeUsed();
            if (range == null) return null;

            int rows = range.LastRow().RowNumber();
            int columns = range.LastColumn().ColumnNumber();
            object[,] values = new object[rows, columns];
            foreach (IXLCell cell in range.CellsUsed())
            {
                values[cell.Address.RowNumber - 1, cell.Address.ColumnNumber - 1] = cell.GetString();
            }

            return values;
        }

        public static int GetArrayRows(object[,] values)
        {
            return values == null ? 0 : values.GetLength(0);
        }

        public static int GetArrayColumns(object[,] values)
        {
            return values == null ? 0 : values.GetLength(1);
        }

        public static object GetArrayValue(object[,] values, int row, int column)
        {
            if (values == null || row <= 0 || column <= 0) return null;
            int actualRow = values.GetLowerBound(0) + row - 1;
            int actualColumn = values.GetLowerBound(1) + column - 1;
            if (actualRow < values.GetLowerBound(0) || actualRow > values.GetUpperBound(0)) return null;
            if (actualColumn < values.GetLowerBound(1) || actualColumn > values.GetUpperBound(1)) return null;
            return values.GetValue(actualRow, actualColumn);
        }

        public static string ReadArrayString(object[,] values, int row, int column)
        {
            object value = GetArrayValue(values, row, column);
            return value == null ? "" : value.ToString();
        }

        public static int ReadArrayInt(object[,] values, int row, int column, int fallback)
        {
            double value = ReadArrayDouble(values, row, column, fallback);
            return (int)Math.Round(value);
        }

        public static double ReadArrayDouble(object[,] values, int row, int column, double fallback)
        {
            string text = ReadArrayString(values, row, column);
            double value;
            if (double.TryParse(text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }

            return fallback;
        }

        public static string ReadString(IXLWorksheet sheet, int row, int column)
        {
            return ReadString(new ExcelWorksheetShim(sheet), row, column);
        }

        public static string ReadString(ExcelWorksheetShim sheet, int row, int column)
        {
            if (column <= 0) return "";
            if (sheet == null || sheet.Worksheet == null) return "";
            IXLCell cell = sheet.Worksheet.Cell(row, column);
            return cell == null || cell.IsEmpty() ? "" : cell.GetString();
        }

        public static int ReadInt(IXLWorksheet sheet, int row, int column, int fallback)
        {
            return ReadInt(new ExcelWorksheetShim(sheet), row, column, fallback);
        }

        public static int ReadInt(ExcelWorksheetShim sheet, int row, int column, int fallback)
        {
            double value = ReadDouble(sheet, row, column, fallback);
            return (int)Math.Round(value);
        }

        public static double ReadDouble(ExcelWorksheetShim sheet, int row, int column, double fallback)
        {
            if (sheet == null || sheet.Worksheet == null) return fallback;
            IXLCell cell = sheet.Worksheet.Cell(row, column);
            double directValue;
            if (cell != null && cell.TryGetValue<double>(out directValue))
            {
                return directValue;
            }

            string text = ReadString(sheet, row, column);
            double value;
            if (double.TryParse(text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }

            return fallback;
        }

        public static bool TryReadDouble(IXLCell cell, out double value)
        {
            if (cell != null && cell.TryGetValue<double>(out value)) return true;
            string text = cell == null ? "" : cell.GetString();
            return double.TryParse(text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }
    }

    internal sealed class ExcelCellAccessor
    {
        private readonly IXLWorksheet worksheet;

        public ExcelCellAccessor(IXLWorksheet worksheet)
        {
            this.worksheet = worksheet;
        }

        public object this[int row, int column]
        {
            set
            {
                if (worksheet == null) return;
                ExcelWorksheetShim.WriteClosedXmlValue(worksheet.Cell(row, column), value);
            }
        }
    }
}

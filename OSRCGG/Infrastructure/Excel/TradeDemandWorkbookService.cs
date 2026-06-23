using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;

namespace OSRCGG
{
    internal sealed class TradeDemandWorkbookService
    {
        private readonly IReadOnlyList<string> merchandiseNames;

        public TradeDemandWorkbookService(IEnumerable<string> merchandiseNames)
        {
            this.merchandiseNames = (merchandiseNames ?? Enumerable.Empty<string>()).ToList();
        }

        public double[] LoadDemands(string fileName, int demandCount)
        {
            double[] demands = new double[Math.Max(0, demandCount)];

            using (XLWorkbook workbook = new XLWorkbook(fileName))
            {
                IXLWorksheet worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    throw new InvalidOperationException("Workbook has no worksheets.");
                }

                // Импорт намеренно принимает первые 29 чисел из используемой области:
                // старые пользовательские книги встречаются и горизонтальными, и вертикальными.
                IXLRange usedRange = worksheet.RangeUsed();
                if (usedRange == null) return demands;

                int count = 0;
                foreach (IXLCell cell in usedRange.CellsUsed())
                {
                    if (count >= demands.Length) break;
                    double value;
                    if (ExcelWorksheetShim.TryReadDouble(cell, out value) && value >= -10 && value <= 10)
                    {
                        demands[count] = value;
                        count++;
                    }
                }
            }

            return demands;
        }

        public void SaveDemands(string fileName, double[] demands, IEnumerable<string> headers)
        {
            using (XLWorkbook workbook = new XLWorkbook())
            {
                IXLWorksheet sheet = workbook.Worksheets.Add("Demands");
                string[] headerValues = (headers ?? merchandiseNames).ToArray();
                double[] values = MapDemandService.NormalizeDemandArray(demands);

                // Формат сохранения оставлен простым и совместимым: товары в первой строке, значения во второй.
                int count = Math.Min(headerValues.Length, values.Length);
                for (int i = 0; i < count; i++)
                {
                    sheet.Cell(1, i + 1).SetValue(headerValues[i]);
                    sheet.Cell(2, i + 1).SetValue(values[i]);
                }

                if (count > 0) sheet.Columns(1, count).AdjustToContents();
                workbook.SaveAs(fileName);
            }
        }

    }
}

using ClosedXML.Excel;
using ComputeSlopeSummary.Models;
using Microsoft.Extensions.Logging;
using Models;

namespace ComputeSlopeSummary.Services;

internal class GenerateExcel(ILogger<GenerateExcel> logger, SummaryDbMethods dbMethods)
{
    private readonly ILogger<GenerateExcel> logger = logger;
    private readonly SummaryDbMethods dbMethods = dbMethods;
    private const int YearlySelect = 100;
    private const int MonthlySelect = 10;
    private const int HalfYearlySelect = 50;
    private const int QuarterlySelect = 25;

    public async Task<bool> GenerateExcelAsync()
    {
        string homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string excelDirectory = Path.Combine(homeFolder, "excelDirectory");
        try
        {
            Directory.CreateDirectory(excelDirectory);
            string stringFileName = DateTime.Now.DayOfWeek.ToString() + ".xlsx";
            string excelFilePath = Path.Combine(excelDirectory, stringFileName);
            if (File.Exists(excelFilePath))
            {
                File.Delete(excelFilePath);
            }
            var excelWorkBook = new XLWorkbook();
            Dictionary<string, string> tickerCompanyName = await dbMethods.GetCompanyNamesAsync();
            foreach (Period period in Enum.GetValues(typeof(Period)))
            {
                List<SlopeSummary> slopeSummaries = await dbMethods.GetSlopeSummaryAsync(period);
                List<ExcelStore> sheetData = [];
                foreach (SlopeSummary slopeSummary in slopeSummaries)
                {
                    sheetData.Add(slopeSummary);
                    sheetData.Last().CompanyName = tickerCompanyName[slopeSummary.Ticker] ?? "";
                }
                string sheetName = period.ToString();
                var worksheet = excelWorkBook.Worksheets.Add(sheetName);
                worksheet.ColumnWidth = 25;
                IXLTable table = worksheet.FirstCell().InsertTable(sheetData);
                table.AutoFilter.IsEnabled = true;
                table.AutoFilter.Sort(5, XLSortOrder.Descending);
            }
            excelWorkBook.SaveAs(excelFilePath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("Error creating excel file");
            logger.LogError(message: ex.ToString());
        }
        return false;
    }

    public async Task RecursiveSelection()
    {
        string homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string excelDirectory = Path.Combine(homeFolder, "excelDirectory");
        Directory.CreateDirectory(excelDirectory);
        string stringFileName = "RecursiveSelection.xlsx";
        string excelFilePath = Path.Combine(excelDirectory, stringFileName);
        if (File.Exists(excelFilePath))
        {
            File.Delete(excelFilePath);
        }
        List<SlopeSummary> slopeSummaries = await dbMethods.GetSlopeSummaryAsync(Period.Yearly);
        var selectedTickers = slopeSummaries.OrderByDescending(x => x.PeriodEnd.Slope)
            .Take(YearlySelect)
            .Select(x => x.Ticker)
            .ToList();
        slopeSummaries = await dbMethods.GetSlopeSummaryAsync(Period.HalfYearly);
        slopeSummaries = slopeSummaries.Where(x => selectedTickers.Contains(x.Ticker))
            .OrderByDescending(x => x.PeriodEnd.Slope)
            .Take(HalfYearlySelect)
            .ToList();
        selectedTickers = slopeSummaries.Select(x => x.Ticker).ToList();
        slopeSummaries = await dbMethods.GetSlopeSummaryAsync(Period.Quarterly);
        slopeSummaries = slopeSummaries.Where(x => selectedTickers.Contains(x.Ticker))
            .OrderByDescending(x => x.PeriodEnd.Slope)
            .Take(QuarterlySelect)
            .ToList();
        selectedTickers = slopeSummaries.Select(x => x.Ticker).ToList();
        slopeSummaries = await dbMethods.GetSlopeSummaryAsync(Period.Monthly);
        slopeSummaries = slopeSummaries.Where(x => selectedTickers.Contains(x.Ticker))
            .OrderByDescending(x => x.PeriodEnd.Slope)
            .Take(MonthlySelect)
            .ToList();
        Dictionary<string, string> tickerCompanyName = await dbMethods.GetCompanyNamesAsync();
        List<ExcelStore> sheetData = [];
        foreach (SlopeSummary slopeSummary in slopeSummaries)
        {
            sheetData.Add(slopeSummary);
            sheetData.Last().CompanyName = tickerCompanyName[slopeSummary.Ticker] ?? "";
        }
        var excelWorkBook = new XLWorkbook();
        string sheetName = "RecursiveSelection";
        var worksheet = excelWorkBook.Worksheets.Add(sheetName);
        worksheet.ColumnWidth = 25;
        IXLTable table = worksheet.FirstCell().InsertTable(sheetData);
        table.AutoFilter.IsEnabled = true;
        table.AutoFilter.Sort(5, XLSortOrder.Descending);
        excelWorkBook.SaveAs(excelFilePath);
    }
}
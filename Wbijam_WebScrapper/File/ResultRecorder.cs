using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Serilog;
using Wbijam.WebScrapper.Web;

namespace Wbijam.WebScrapper.File;

public class ResultRecorder : IResultRecorder
{
    const string BASE_PATH = @"E:\DEV\Temp\Scrapped_Anime_RESULT";
    private readonly string _resultDirectoryPath;
    private readonly ILogger _logger;

    public ResultRecorder(ILogger logger)
    {
        _logger = logger;

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var resultDirectoryName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var resultDirectoryPath = Path.Combine(BASE_PATH, resultDirectoryName);

        if (!Directory.Exists(resultDirectoryPath))
        {
            Directory.CreateDirectory(resultDirectoryPath);
            _logger.Information("Created output result directory: {directoryPath}", resultDirectoryPath);
        }

        _resultDirectoryPath = resultDirectoryPath;   
    }

    public async Task SaveResult(AnimeModel scrappedAnime)
    {
        await SaveToJsonFileAsync(scrappedAnime);
        await SaveToExcelFile(scrappedAnime);
    }

    private async Task SaveToExcelFile(AnimeModel scrappedAnime)
    {
        var excelOutputFile = new FileInfo(Path.Combine(_resultDirectoryPath, $"result.xlsx"));
        using var package = new ExcelPackage(excelOutputFile);

        const int headerRow = 1;
        const int titleColumn = 1;
        const int typeColumn = 2;
        const int dateReleaseColumn = 3;
        const int urlsColumn = 5;
         
        var worksheet = package.Workbook.Worksheets.Add(scrappedAnime.Title);

        CreateHeaderRow(headerRow, titleColumn, typeColumn, dateReleaseColumn, urlsColumn, worksheet);

        var currentRowIndex = headerRow;

        foreach (var series in scrappedAnime.Series)
        {
            currentRowIndex++;
            currentRowIndex++;

            worksheet.Cells[currentRowIndex, titleColumn].Value = series.SeriesName;
            worksheet.Cells[currentRowIndex, titleColumn].Style.Font.Bold = true;
            worksheet.Cells[currentRowIndex, titleColumn].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[currentRowIndex, titleColumn].Style.Fill.BackgroundColor.SetColor(1, 160, 204, 130);
            currentRowIndex++;

            foreach (var episode in series.AnimeEpisodes)
            {
                worksheet.Cells[currentRowIndex, titleColumn].Value = episode.EpisodeName;
                worksheet.Cells[currentRowIndex, typeColumn].Value = episode.EpisodeType;
                worksheet.Cells[currentRowIndex, dateReleaseColumn].Value = episode.EpisodeReleaseDateOrRangeOfEpisodes;

                var currentColumnIndex = urlsColumn;

                foreach (var url in episode.EpisodeVideoUrls)
                {
                    if (url.Contains("VKontakte"))
                    {
                        worksheet.Cells[currentRowIndex, currentColumnIndex].Value = url;
                    }
                    else 
                    {
                        worksheet.Cells[currentRowIndex, currentColumnIndex].Hyperlink = new Uri(url);
                    }
                        
                    currentColumnIndex++;
                }

                currentRowIndex++;
            }
        }

        worksheet.Columns[1].AutoFit();
        worksheet.Columns[2].AutoFit();
        worksheet.Columns[3].AutoFit();
        
        await package.SaveAsync();

        _logger.Information("Saved anime: {animeName} to output excel file: {excelFilePath}", scrappedAnime.Title, excelOutputFile.FullName);
    }

    private static void CreateHeaderRow(int headerRow, int titleColumn, int typeColumn, int dateReleaseColumn, int urlsColumn, ExcelWorksheet worksheet)
    {
        worksheet.Cells[headerRow, titleColumn].Value = "Tytuł";
        worksheet.Cells[headerRow, titleColumn].Style.Font.Bold = true;
        worksheet.Cells[headerRow, titleColumn].Style.Border.BorderAround(ExcelBorderStyle.Thin);
        worksheet.Cells[headerRow, typeColumn].Value = "Typ";
        worksheet.Cells[headerRow, typeColumn].Style.Font.Bold = true;
        worksheet.Cells[headerRow, typeColumn].Style.Border.BorderAround(ExcelBorderStyle.Thin);
        worksheet.Cells[headerRow, dateReleaseColumn].Value = "Data wydania / zakres";
        worksheet.Cells[headerRow, dateReleaseColumn].Style.Font.Bold = true;
        worksheet.Cells[headerRow, dateReleaseColumn].Style.Border.BorderAround(ExcelBorderStyle.Thin);

        worksheet.Cells[headerRow, urlsColumn].Value = "Linki do playerów";
        worksheet.Cells[headerRow, urlsColumn].Style.Font.Bold = true;
        worksheet.Cells[headerRow, urlsColumn].Style.Border.BorderAround(ExcelBorderStyle.Thin);


        for (int i = urlsColumn; i <= 12; i++)
        {
            worksheet.Columns[i].Width = 35;
        }
    }

    private async Task SaveToJsonFileAsync(AnimeModel scrappedAnime)
    {
        var filePath = Path.Combine(_resultDirectoryPath, $"{scrappedAnime.Title}.json");
        var serializedResult = JsonConvert.SerializeObject(scrappedAnime, Formatting.Indented);

        await System.IO.File.WriteAllTextAsync(filePath, serializedResult);

        _logger.Information("Saved anime: {animeName} to a json file: {filePath}", scrappedAnime.Title, filePath);
    }
}

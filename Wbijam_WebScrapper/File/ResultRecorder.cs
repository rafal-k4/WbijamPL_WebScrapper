using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Wbijam.WebScrapper.Web;

namespace Wbijam.WebScrapper.File;

public class ResultRecorder : IResultRecorder
{
    const string RESULT_DIR = @"E:\DEV\Temp\Scrapped_Anime_RESULT";

    public ResultRecorder()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task SaveResult(List<AnimeModel> scrappedAnimes)
    {
        await SaveToJsonFileAsync(scrappedAnimes);
        await SaveToExcelFile(scrappedAnimes);
    }

    private async Task SaveToExcelFile(List<AnimeModel> scrappedAnimes)
    {
        using var package = new ExcelPackage();

        const int headerRow = 1;
        const int titleColumn = 1;
        const int typeColumn = 2;
        const int dateReleaseColumn = 3;
        const int urlsColumn = 5;
         
        foreach (var anime in scrappedAnimes)
        {
            var worksheet = package.Workbook.Worksheets.Add(anime.Title);

            CreateHeaderRow(headerRow, titleColumn, typeColumn, dateReleaseColumn, urlsColumn, worksheet);

            var currentRowIndex = headerRow;

            foreach (var series in anime.Series)
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
        }

        await package.SaveAsAsync(new FileInfo(Path.Combine(RESULT_DIR, $"{DateTime.Now:yyyyMMdd_HHmmss}_anime.xlsx")));
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

    private async Task SaveToJsonFileAsync(List<AnimeModel> scrappedAnimes)
    {
        if (!Directory.Exists(RESULT_DIR))
            Directory.CreateDirectory(RESULT_DIR);

        var filePath = Path.Combine(RESULT_DIR, $"anime_list_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        var serializedResult = JsonConvert.SerializeObject(scrappedAnimes, Formatting.Indented);

        await System.IO.File.WriteAllTextAsync(filePath, serializedResult);
    }
}

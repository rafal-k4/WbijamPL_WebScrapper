using PuppeteerSharp;
using PuppeteerSharp.Dom;
using Serilog;

namespace Wbijam.WebScrapper.Web;

public class WebScrapper : IWebScrapper
{
    private const string WBIJAM_URL = @"https://wbijam.pl/";
    private readonly ILogger _logger;

    public WebScrapper(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<List<AnimeModel>> GetAnimeDataAsync()
    {
        _logger.Information("Starting headless browser...");
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        await using var page = await browser.NewPageAsync();

        _logger.Information("Navigating to {webpage}", WBIJAM_URL);
        await page.GoToAsync(WBIJAM_URL);

        var animeAnchors = await GetAnimesUrls(page);
        return await NavigateAndWebScrapAnimesData(page, animeAnchors);
    }

    private static async Task<IElementHandle[]> GetAnimesUrls(IPage page)
    {
        var footerWithAnimeNames = await page.QuerySelectorAsync("div#stopka_srodek");

        var animeAnchors = await footerWithAnimeNames.QuerySelectorAllAsync("a");
        return animeAnchors;
    }

    private async Task<List<AnimeModel>> NavigateAndWebScrapAnimesData(IPage page, IElementHandle[] animeAnchors)
    {
        var animes = new List<AnimeModel>();

        _logger.Information("{anime_count} animes found", animeAnchors.Length);

        foreach (var animeAnchor in animeAnchors)
        {
            string animeUrl = await NavigateToGivenAnimePage(page, animeAnchor);

            var seriesUrls = await GetAnimeSeriesUrls(page, animeUrl);

            ;

        }

        return animes;
    }

    private async Task<List<string>> GetAnimeSeriesUrls(IPage page, string animeUrl)
    {
        var subMenus = await page.QuerySelectorAllAsync<HtmlElement>("div.pmenu_naglowek_b");
        var animeSeriesDiv = await GetFirstElementContainingText(subMenus, "Odcinki anime online");

        if (animeSeriesDiv is null)
            throw new Exception("Couldn't find anime series html element");

        var animeSeriesSiblingList = await animeSeriesDiv.GetNextElementSiblingAsync<HtmlUnorderedListElement>();

        if (animeSeriesSiblingList is null)
            throw new Exception("Couldn't find anime series list");

        var allListElements = await animeSeriesSiblingList.GetChildrenAsync<HtmlListItemElement>();

        var allAnimeSeriesUrls = new List<string>();
        await foreach (var listEl in allListElements)
        {
            var imageIcon = await listEl.QuerySelectorAsync<HtmlElement>("img");
            if (await imageIcon.GetAttributeAsync("src") == "images/tv_info.gif")
            {
                var animeSeriesAnchorElement = await listEl.QuerySelectorAsync<HtmlElement>("a");
                var animeSeriesUrl = await animeSeriesAnchorElement.GetAttributeAsync("href");

                if (string.IsNullOrWhiteSpace(animeSeriesUrl))
                    throw new Exception($"Couldn't find series url for: {animeUrl}");

                allAnimeSeriesUrls.Add(animeSeriesUrl);
            }
        }

        return allAnimeSeriesUrls;
    }

    private async Task<string> NavigateToGivenAnimePage(IPage page, IElementHandle animeAnchor)
    {
        var animeUrl = await animeAnchor.EvaluateFunctionAsync<string>("a => a.href");
        await NavigateToPageAsync(page, animeUrl);

        var anchorElementForSubpageWithAnimeEpisodes = await page.QuerySelectorAsync("center a.sub_link");
        var urlToSubpageWithEpisodes = await anchorElementForSubpageWithAnimeEpisodes.EvaluateFunctionAsync<string>("a => a.href");

        await NavigateToPageAsync(page, urlToSubpageWithEpisodes);
        return animeUrl;
    }

    private async Task<HtmlElement?> GetFirstElementContainingText(HtmlElement[] elements, string innerText)
    {
        foreach(var element in elements)
        {
            if ((await element.GetInnerTextAsync()) == innerText)
                return element;
        }

        return null;
    }

    private async Task NavigateToPageAsync(IPage page, string animeUrl)
    {
        const int delayMiliseconds = 500;
        _logger.Information("Navigatin to page: {anime_web_page}", animeUrl);
        await Task.Delay(delayMiliseconds); // not sure if it can block IP due to bot traversing pages
        await page.GoToAsync(animeUrl);
    }
}

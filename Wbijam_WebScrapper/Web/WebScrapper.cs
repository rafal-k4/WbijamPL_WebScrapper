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

        var allAnimesUrls = animeAnchors.Select(anchor => anchor.EvaluateFunctionAsync<string>("a => a.href")).ToList();

        foreach (var animeUrl in allAnimesUrls)
        {
            string animeSubDomainUrl = await NavigateToGivenAnimePageAsync(page, await animeUrl);

            var animeModel = new AnimeModel();
            animeModel.Title = await GetAnimeTitleAsync(page);

            var seriesUrlsaths = await GetAnimeSeriesUrlsPathsAsync(page, animeSubDomainUrl);

            await PopulateAnimeEpisodesAsync(page, animeSubDomainUrl, seriesUrlsaths, animeModel);
        }

        return animes;
    }

    private async Task<string> GetAnimeTitleAsync(IPage page)
    {
        var pageTitle = await page.GetTitleAsync();

        var animeTitle = pageTitle.Split("- wszystkie odcinki anime online");

        if (!animeTitle.Any())
        {
            _logger.Error("Couldn't find anime title: {page_title}", pageTitle);
            throw new Exception($"Couldn't find anime title: {pageTitle}");
        }

        return animeTitle[0];
    }

    private async Task PopulateAnimeEpisodesAsync(IPage page, string animeSubdomain, List<string> seriesUrls, AnimeModel animeModel)
    {
        foreach(var seriesUrl in seriesUrls)
        {
            await NavigateToPageAsync(page, new Uri(new Uri(animeSubdomain), seriesUrl).ToString());


        }
    }

    private async Task<List<string>> GetAnimeSeriesUrlsPathsAsync(IPage page, string animeUrl)
    {
        var subMenus = await page.QuerySelectorAllAsync<HtmlElement>("div.pmenu_naglowek_b");
        var animeSeriesDiv = await GetFirstElementContainingText(subMenus, "Odcinki anime online");

        if (animeSeriesDiv is null)
        {
            _logger.Error("Couldn't find anime series html element, anime url: {anime_url}", animeUrl);
            throw new Exception("Couldn't find anime series html element");
        }

        var animeSeriesSiblingList = await animeSeriesDiv.GetNextElementSiblingAsync<HtmlUnorderedListElement>();

        if (animeSeriesSiblingList is null)
        {
            _logger.Error("Couldn't find anime series list, anime url: {anime_url}", animeUrl);
            throw new Exception("Couldn't find anime series list");
        }
            

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
                {
                    _logger.Error("Couldn't find series url for: {anime_url}", animeUrl);
                    throw new Exception($"Couldn't find series url for: {animeUrl}");
                }
                    
                allAnimeSeriesUrls.Add(animeSeriesUrl);
            }
        }

        if (!allAnimeSeriesUrls.Any())
        {
            _logger.Warning("Didn't find any anime series, for the following anime: {anime_name}", animeUrl);
            return allAnimeSeriesUrls;
        }

        _logger.Information("For anime: {anime_name}, found following anime series: {anime_series}", animeUrl, string.Join(", ", allAnimeSeriesUrls));

        return allAnimeSeriesUrls;
    }

    private async Task<string> NavigateToGivenAnimePageAsync(IPage page, string animeUrl)
    {
        await NavigateToPageAsync(page, animeUrl);

        var anchorElementForSubpageWithAnimeEpisodes = await page.QuerySelectorAsync("center a.sub_link");
        var urlToSubpageWithEpisodes = await anchorElementForSubpageWithAnimeEpisodes.EvaluateFunctionAsync<string>("a => a.href");

        await NavigateToPageAsync(page, urlToSubpageWithEpisodes);

        if (string.IsNullOrWhiteSpace(urlToSubpageWithEpisodes))
        {
            _logger.Error("Couldn't find anime subdomain url: {animeUrl}", animeUrl);
            throw new Exception($"Couldn't find anime subdomain url: {animeUrl}");
        }
            
        return urlToSubpageWithEpisodes;
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

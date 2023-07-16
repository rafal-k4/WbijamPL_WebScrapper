﻿using Polly;
using Polly.Retry;
using Polly.Wrap;
using PuppeteerSharp;
using PuppeteerSharp.Dom;
using Serilog;

namespace Wbijam.WebScrapper.Web;

public class WebScrapper : IWebScrapper
{
    private const string WBIJAM_URL = @"https://wbijam.pl/";
    private readonly ILogger _logger;

    private AsyncRetryPolicy _retryNavigationPolicy;
    private AsyncPolicyWrap<string?> _retryPolicyForScrappingVideoSrc;

    public WebScrapper(ILogger logger)
    {
        _logger = logger;

        _retryNavigationPolicy = Policy.Handle<NavigationException>()
            .WaitAndRetryAsync(new[]
              {
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30)
              }, (exception, timeSpan, retryCount, context) => {
                  _logger.Error("Retry attempt: {retryCount} - Encountered exception while trying to navigate to page: {url}", retryCount, context["url"]);
              });

        var scrappingVideoPlayerSourceUriRetryPolicy = Policy.Handle<VideoSrcUrlNotFound>()
           .WaitAndRetryAsync(new[]
             {
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(4)
             }, (exception, timeSpan, retryCount, context) => {
                 _logger.Error("Retry attempt: {retryCount} - Didn't find video element for this episode: {episodeName}, anime name: {animeName}, series: {seriesName}", retryCount, context["episodeName"], context["animeName"], context["seriesName"]);
             });

        var fallbackPolicy = Policy<string?>.Handle<VideoSrcUrlNotFound>()
            .FallbackAsync<string?>((string?)null, onFallbackAsync: (result, context) =>
            {
                _logger.Warning("Failed to get video source url - episode: {episodeName}, anime name: {animeName}, series: {seriesName}", context["episodeName"], context["animeName"], context["seriesName"]);
                return Task.CompletedTask;
            });

        _retryPolicyForScrappingVideoSrc = fallbackPolicy.WrapAsync<string?>(scrappingVideoPlayerSourceUriRetryPolicy);
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

            var seriesUrlsaths = await GetAnimeSeriesUrlsPathsAsync(page, animeSubDomainUrl, animeModel);

            await PopulateAnimeEpisodesAsync(page, animeSubDomainUrl, animeModel);

            animes.Add(animeModel);
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

    private async Task PopulateAnimeEpisodesAsync(IPage page, string animeSubdomain, AnimeModel animeModel)
    {
        foreach(var animeSeries in animeModel.Series)
        {
            await NavigateToPageAsync(page, new Uri(new Uri(animeSubdomain), animeSeries.SeriesUrlPath).ToString());

            var tableWithEpisodes = await page.QuerySelectorAsync<HtmlTableElement>("table.lista");
            var episodeRows = await tableWithEpisodes.GetRowsAsync().ToArrayAsync();

            if (!episodeRows.Any())
                _logger.Warning("No episodes found for this anime: {animeName}, series: {seriesName}", animeModel.Title, animeSeries.SeriesUrlPath);

            foreach (var row in episodeRows)
            { 
                var columns = await row.GetCellsAsync().ToArrayAsync();

                if (columns.Length < 3)
                {
                    _logger.Error("Anime: {anime_name}, series: {anime_series}, does not contain required 3 columns containing - name, type and release date.",
                        animeModel.Title,
                        animeSeries.SeriesName);
                    throw new Exception($"Anime: {animeModel.Title}, series: {animeSeries.SeriesName}, does not contain required 3 columns containing - name, type and release date.");
                }

                var animeEpisode = new AnimeEpisode();

                if (animeSeries.SeriesName.ToLowerInvariant() == "openingi"
                    || animeSeries.SeriesName.ToLowerInvariant() == "endingi")
                {
                    animeEpisode.EpisodeName = await columns[0].GetInnerTextAsync();
                    animeEpisode.EpisodeReleaseDateOrRangeOfEpisodes = await columns[1].GetInnerTextAsync();
                    animeEpisode.EpisodeType = await columns[2].GetInnerTextAsync();
                    
                } else
                {
                    animeEpisode.EpisodeName = await columns[0].GetInnerTextAsync();
                    animeEpisode.EpisodeType = await columns[1].GetInnerTextAsync();
                    animeEpisode.EpisodeReleaseDateOrRangeOfEpisodes = await columns[2].GetInnerTextAsync();
                }

                var episodePlayerUrlAnchorElement = await columns[0].QuerySelectorAsync<HtmlAnchorElement>("a");
                animeEpisode.EpisodePlayersUrlPath = await episodePlayerUrlAnchorElement.GetAttributeAsync<string>("href");

                animeSeries.AnimeEpisodes.Add(animeEpisode);
            }

            foreach (var episode in animeSeries.AnimeEpisodes)
            {
                episode.EpisodeVideoUrls = await GetAnimeEpisodes(
                    page,
                    animeSubdomain,
                    episode.EpisodePlayersUrlPath,
                    episode.EpisodeName,
                    animeModel.Title,
                    animeSeries.SeriesName);
            }
        }        
    }

    private async Task<List<string>> GetAnimeEpisodes(IPage page, string animeSubdomain, string episodePlayersUrl, string episodeName, string title, string seriesName)
    {
        var playersUrls = new List<string>();
        await NavigateToPageAsync(page, new Uri(new Uri(animeSubdomain), episodePlayersUrl).ToString());

        var playersTable = await page.QuerySelectorAsync<HtmlTableElement>("table.lista");
        var playerRows = await playersTable.GetRowsAsync().ToArrayAsync();

        if (!playerRows.Any())
        {
            _logger.Warning("No players found for this episode: {episodeName}, anime name: {animeName}, series: {seriesName}", episodeName, title, seriesName);
        }

        var videoPlayerPageUrls = new List<string>();

        foreach(var row in playerRows)
        {
            var headerRow = await row.QuerySelectorAsync("th");
            if (headerRow is not null)
                continue;

            var playerPageElement = await row.QuerySelectorAsync<HtmlElement>("span");
            var playerPageUrlPathPart = await playerPageElement.GetAttributeAsync<string>("rel");

            var playerPageUrl = new Uri(new Uri(animeSubdomain), $"odtwarzacz-{playerPageUrlPathPart}.html").ToString();

            videoPlayerPageUrls.Add(playerPageUrl);
        }

        foreach(var playerPageUrl in videoPlayerPageUrls)
        {
            var videoPlayerBaseUrl = await GetVideoPlayerUrl(page, playerPageUrl, episodeName, title, seriesName);

            if (videoPlayerBaseUrl == null)
                continue;

            playersUrls.Add(videoPlayerBaseUrl);
        }

        return playersUrls;
    }

    private async Task<string?> GetVideoPlayerUrl(IPage page, string playerPageUrl, string episodeName, string title, string seriesName)
    {
        await NavigateToPageAsync(page, playerPageUrl, true);

        var warningMessageElement = await page.QuerySelectorAsync<HtmlElement>("center");
        
        if (warningMessageElement != null)
        {
            var warningMessage = await warningMessageElement.GetInnerTextAsync();
            if (warningMessage is not null && warningMessage.Contains("Aby oglądać odcinki na serwerze VK"))
                return "VKontakte player -> https://vk.com/";
        }

        Dictionary<string, object> retryContextData = new()
        {
            { "episodeName", episodeName },
            { "animeName", title },
            { "seriesName", seriesName },
        };

        return await _retryPolicyForScrappingVideoSrc.ExecuteAsync(_ => GetVideoPlayerSourceUrl(page), retryContextData);

        async Task<string?> GetVideoPlayerSourceUrl(IPage page)
        {
            var videoIframe = await page.QuerySelectorAsync<HtmlElement>("iframe");

            if (videoIframe is null)
            {
                throw new VideoSrcUrlNotFound();
            }

            var videoPlayerBaseUrl = await videoIframe.GetAttributeAsync<string>("src");

            return videoPlayerBaseUrl;
        }
    }

    private async Task<List<string>> GetAnimeSeriesUrlsPathsAsync(IPage page, string animeUrl, AnimeModel animeModel)
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

        var allAnimeSeriesUrls = new List<KeyValuePair<string, string>>();
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

                var seriesName = await animeSeriesAnchorElement.GetInnerTextAsync();

                allAnimeSeriesUrls.Add(new KeyValuePair<string, string>(animeSeriesUrl, seriesName));
            }
        }

        if (!allAnimeSeriesUrls.Any())
        {
            _logger.Warning("Didn't find any anime series, for the following anime: {anime_name}", animeUrl);
            return allAnimeSeriesUrls.Select(x => x.Key).ToList();
        }

        _logger.Information("For anime: {anime_name}, found following anime series: {anime_series}", animeUrl, string.Join(", ", allAnimeSeriesUrls));

        animeModel.Series = allAnimeSeriesUrls.Select(x => new AnimeSeries
        {
            SeriesName = x.Value,
            SeriesUrlPath = x.Key
        }).ToList();

        return allAnimeSeriesUrls.Select(x => x.Key).ToList();
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

    private async Task NavigateToPageAsync(IPage page, string url, bool waitUntilNetworkIdle = false)
    {
        const int delayMiliseconds = 500;
        _logger.Information("Navigatin to page: {anime_web_page}", url);

        await Task.Delay(delayMiliseconds); // not sure if it can block IP due to bot traversing pages

        Dictionary<string, object> retryContextData = new()
        {
            { "url", url }
        };

        if (waitUntilNetworkIdle)
        {
            await _retryNavigationPolicy.ExecuteAsync(_ => page.GoToAsync(url, WaitUntilNavigation.Networkidle0), retryContextData);
        } else
        {
            await _retryNavigationPolicy.ExecuteAsync(_ => page.GoToAsync(url), retryContextData);
        }    
    }
}

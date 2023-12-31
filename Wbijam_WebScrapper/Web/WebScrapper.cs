﻿using Polly;
using Polly.Retry;
using Polly.Wrap;
using PuppeteerSharp;
using PuppeteerSharp.Dom;
using Serilog;

namespace Wbijam.WebScrapper.Web;

public class WebScrapper : IWebScrapper, IAsyncDisposable
{
    private const string WBIJAM_URL = @"https://wbijam.pl/";
    private readonly ILogger _logger;

    private AsyncRetryPolicy _retryNavigationPolicy;
    private AsyncRetryPolicy _waitingForElementBeingLoadedPolicy;
    private AsyncPolicyWrap<string?> _retryPolicyForScrappingVideoSrc;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public WebScrapper(ILogger logger)
    {
        _logger = logger;

        _retryNavigationPolicy = Policy.Handle<NavigationException>()
            .WaitAndRetryAsync(new[]
              {
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(120)
              }, async (exception, timeSpan, retryCount, context) => {
                  _logger.Error("Retry attempt: {retryCount} - Encountered exception while trying to navigate to page: {url}", retryCount, context["url"]);
                  await DisposeAsync();
                  await InitalizeBrowserAsync();
              });

        _waitingForElementBeingLoadedPolicy = Policy.Handle<PuppeteerException>()
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromSeconds(60)
            }, async (exception, timeSpan, retryCount, _) => {
                _logger.Error("Retry attempt: {retryCount} - Encountered exception while trying to scrap player source url", retryCount);
                await DisposeAsync();
                await InitalizeBrowserAsync();
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

    public async Task GetAnimeDataAsync(Func<AnimeModel, Task> scrappedAnimeDelegate)
    {
        await InitalizeBrowserAsync();

        _logger.Information("Navigating to {webpage}", WBIJAM_URL);
        await _page.GoToAsync(WBIJAM_URL);

        var animeAnchors = await GetAnimesUrls();
        await NavigateAndWebScrapAnimesData(animeAnchors, scrappedAnimeDelegate);
    }

    private async Task InitalizeBrowserAsync()
    {
        _logger.Information("Starting headless browser...");
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        _browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = false });
        _page = await _browser.NewPageAsync();
    }

    private async Task<IElementHandle[]> GetAnimesUrls()
    {
        var footerWithAnimeNames = await _page.QuerySelectorAsync("div#stopka_srodek");

        var animeAnchors = await footerWithAnimeNames.QuerySelectorAllAsync("a");
        return animeAnchors;
    }

    private async Task NavigateAndWebScrapAnimesData(IElementHandle[] animeAnchors, Func<AnimeModel, Task> func)
    {
        _logger.Information("{anime_count} animes found", animeAnchors.Length);

        var allAnimesUrls = await Task.WhenAll(animeAnchors.Select(anchor => anchor.EvaluateFunctionAsync<string>("a => a.href")));
        
        const string startFrom = "bleach";
        var animesUrlsToScrap = allAnimesUrls.SkipWhile(x => !x.Contains(startFrom)).ToList();

        _logger.Information("Starting from anime: {animeName}, {anime_count} left animes to scrap", startFrom, animesUrlsToScrap.Count);

        foreach (var animeUrl in animesUrlsToScrap)
        {
            var isOtherAnimeSection = animeUrl.Contains("inne.wbijam.pl");

            string animeSubDomainUrl = isOtherAnimeSection 
                ? animeUrl
                : await NavigateToGivenAnimePageAsync(animeUrl);

            var animeModel = new AnimeModel();

            if(isOtherAnimeSection)
            {
                await NavigateToPageAsync(animeSubDomainUrl);
                List<AnimeModel> otherAnimes = await GetOtherAnimesPagePathsAsync(animeSubDomainUrl);
                
                foreach(var otherAnime in otherAnimes)
                {
                    if (!otherAnime.SeriesPagesPaths.Any())
                        _logger.Warning("Other anime {animeName} doesn't have correlating subpage url", otherAnime.Title);

                    await PopulateAnimeEpisodesAsync(new Uri(new Uri(animeSubDomainUrl), otherAnime.SeriesPagesPaths[0]).ToString(), otherAnime, isOtherAnimeSection);

                    await func(otherAnime);
                }
            } else
            {
                animeModel.Title = await GetAnimeTitleAsync();
                await PopulateAnimeSeriesUrlsPathsAsync(animeSubDomainUrl, animeModel);
                await PopulateAnimeEpisodesAsync(animeSubDomainUrl, animeModel);

                await func(animeModel);
            }
        }
    }

    private async Task<string> GetAnimeTitleAsync()
    {
        var pageTitle = await _page.GetTitleAsync();

        var animeTitle = pageTitle.Split("- wszystkie odcinki anime online");

        if (!animeTitle.Any())
        {
            _logger.Error("Couldn't find anime title: {page_title}", pageTitle);
            throw new Exception($"Couldn't find anime title: {pageTitle}");
        }

        return animeTitle[0].Trim();
    }

    private async Task PopulateAnimeEpisodesAsync(string animeSubdomain, AnimeModel animeModel, bool isOtherAnimeSubdomain = false)
    {
        foreach (var animeSeriesPagePath in animeModel.SeriesPagesPaths)
        {
            await NavigateToPageAsync(new Uri(new Uri(animeSubdomain), animeSeriesPagePath).ToString());

            var seriesHeaders = await _page.QuerySelectorAllAsync<HtmlElement>("h1.pod_naglowek");

            if (!seriesHeaders.Any())
            {
                seriesHeaders = await _page.QuerySelectorAllAsync<HtmlElement>(".naglowek_fb");
            }

            foreach(var header in seriesHeaders)
            {
                var animeSeries = new AnimeSeries
                {
                    SeriesName = (await header.GetInnerTextAsync()).Trim(),
                    SeriesUrlPath = animeSeriesPagePath
                };

                var tableWithEpisodes = await header.GetNextElementSiblingAsync<HtmlTableElement>();

                if (tableWithEpisodes is null)
                {
                    _logger.Warning("Didn't find any sibling element next to a header series: {seriesName}, in anime: {animeName}", animeSeries.SeriesName, animeModel.Title);
                    continue;
                }

                var episodeRows = await tableWithEpisodes.GetRowsAsync().ToArrayAsync();

                if (!episodeRows.Any())
                    _logger.Warning("No episodes found for this anime: {animeName}, series: {seriesName}", animeModel.Title, animeSeries.SeriesName);

                _logger.Information("Found {episodeCount} episodes for this series: {seriesName}", episodeRows.Length, animeSeries.SeriesName);

                var animeEpisodes = isOtherAnimeSubdomain 
                    ? await GetOtherAnimeEpisodes(animeModel, animeSeries.SeriesName, episodeRows, animeSubdomain)
                    : await GetAnimeEpisodes(animeModel, animeSeries.SeriesName, episodeRows);

                animeSeries.AnimeEpisodes = animeEpisodes;

                animeModel.Series.Add(animeSeries);
            }
        }

        foreach(var series in animeModel.Series)
        {
            foreach (var episode in series.AnimeEpisodes)
            {
                _logger.Information("Scrapping player urls for episode: {episodeName}", episode.EpisodeName);

                episode.EpisodeVideoUrls = isOtherAnimeSubdomain 
                    ? await GetOtherAnimeEpisodesUrlsAsync(
                        episode.PlayersUrlPaths,
                        episode.EpisodeName,
                        animeModel.Title,
                        series.SeriesName)
                    : await GetAnimeEpisodesUrlsAsync(
                        animeSubdomain,
                        episode.EpisodePlayersUrlPath,
                        episode.EpisodeName,
                        animeModel.Title,
                        series.SeriesName);
            }
        }
    }

    private async Task<List<AnimeEpisode>> GetOtherAnimeEpisodes(
        AnimeModel animeModel, 
        string seriesName, 
        HtmlTableRowElement[] episodeRows, 
        string animeSubdomain)
    {
        var animeEpisodes = new List<AnimeEpisode>();

        foreach (var row in episodeRows)
        {
            var columns = await row.GetCellsAsync().ToArrayAsync();

            if (columns.Length < 3)
            {
                _logger.Error("Anime: {anime_name}, series: {anime_series}, does not contain required 3 columns containing - name, type and release date.",
                    animeModel.Title,
                    seriesName);
                throw new Exception($"Anime: {animeModel.Title}, series: {seriesName}, does not contain required 3 columns containing - name, type and release date.");
            }

            var animeEpisode = new AnimeEpisode();

            animeEpisode.EpisodeName = (await columns[0].GetInnerTextAsync()).Trim();

            var episodeColumns = await row.GetCellsAsync().ToArrayAsync();

            for (var i = 1; i < episodeColumns.Length; i++)
            {
                var playerPageElement = await episodeColumns[i].QuerySelectorAsync<HtmlSpanElement>("span");
                var episodePlayerUrl = await GetPlayerPageUrlFromSpanElementAsync(animeSubdomain, playerPageElement);
                animeEpisode.PlayersUrlPaths.Add(episodePlayerUrl);
            }

            animeEpisodes.Add(animeEpisode);
        }

        return animeEpisodes;
    }

    private async Task<List<AnimeEpisode>> GetAnimeEpisodes(AnimeModel animeModel, string seriesName, HtmlTableRowElement[] episodeRows)
    {
        var animeEpisodes = new List<AnimeEpisode>();

        foreach (var row in episodeRows)
        {
            var columns = await row.GetCellsAsync().ToArrayAsync();

            if (columns.Length < 3)
            {
                _logger.Error("Anime: {anime_name}, series: {anime_series}, does not contain required 3 columns containing - name, type and release date.",
                    animeModel.Title,
                    seriesName);
                throw new Exception($"Anime: {animeModel.Title}, series: {seriesName}, does not contain required 3 columns containing - name, type and release date.");
            }

            var animeEpisode = new AnimeEpisode();

            if (seriesName.ToLowerInvariant().Contains("openingi")
                || seriesName.ToLowerInvariant().Contains("endingi"))
            {
                animeEpisode.EpisodeName = (await columns[0].GetInnerTextAsync()).Trim();
                animeEpisode.EpisodeReleaseDateOrRangeOfEpisodes = await columns[1].GetInnerTextAsync();
                animeEpisode.EpisodeType = await columns[2].GetInnerTextAsync();

            }
            else
            {
                animeEpisode.EpisodeName = (await columns[0].GetInnerTextAsync()).Trim();
                animeEpisode.EpisodeType = await columns[1].GetInnerTextAsync();
                animeEpisode.EpisodeReleaseDateOrRangeOfEpisodes = await columns[2].GetInnerTextAsync();
            }

            var episodePlayerUrlAnchorElement = await columns[0].QuerySelectorAsync<HtmlAnchorElement>("a");
            animeEpisode.EpisodePlayersUrlPath = await episodePlayerUrlAnchorElement.GetAttributeAsync<string>("href");

            animeEpisodes.Add(animeEpisode);
        }

        return animeEpisodes;
    }


    private async Task<List<string>> GetOtherAnimeEpisodesUrlsAsync(List<string> playersUrlPaths, string episodeName, string title, string seriesName)
    {
        var playersUrls = new List<string>();

        foreach(var playerUrl in playersUrlPaths)
        {
            var videoPlayerBaseUrl = await _waitingForElementBeingLoadedPolicy
                .ExecuteAsync(() => GetVideoPlayerUrlAsync(playerUrl, episodeName, title, seriesName));

            if (videoPlayerBaseUrl == null)
                continue;

            playersUrls.Add(videoPlayerBaseUrl);
        }

        return playersUrls;
    }

    private async Task<List<string>> GetAnimeEpisodesUrlsAsync(string animeSubdomain, string episodePlayersUrl, string episodeName, string title, string seriesName)
    {
        var playersUrls = new List<string>();
        await NavigateToPageAsync(new Uri(new Uri(animeSubdomain), episodePlayersUrl).ToString());

        var playersTable = await _page.QuerySelectorAsync<HtmlTableElement>("table.lista");
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

            var playerPageElement = await row.QuerySelectorAsync<HtmlSpanElement>("span");
            string playerPageUrl = await GetPlayerPageUrlFromSpanElementAsync(animeSubdomain, playerPageElement);
            videoPlayerPageUrls.Add(playerPageUrl);
        }

        foreach (var playerPageUrl in videoPlayerPageUrls)
        {
            var videoPlayerBaseUrl = await _waitingForElementBeingLoadedPolicy
                .ExecuteAsync(() => GetVideoPlayerUrlAsync(playerPageUrl, episodeName, title, seriesName));

            if (videoPlayerBaseUrl == null)
                continue;

            playersUrls.Add(videoPlayerBaseUrl);
        }

        return playersUrls;
    }

    private static async Task<string> GetPlayerPageUrlFromSpanElementAsync(string animeSubdomain, HtmlSpanElement spanElement)
    {
        var playerPageUrlPathPart = await spanElement.GetAttributeAsync<string>("rel");

        var playerPageUrl = new Uri(new Uri(animeSubdomain), $"odtwarzacz-{playerPageUrlPathPart}.html").ToString();
        return playerPageUrl;
    }

    private async Task<string?> GetVideoPlayerUrlAsync(string playerPageUrl, string episodeName, string title, string seriesName)
    {
        await NavigateToPageAsync(playerPageUrl, true);

        await _page.WaitForSelectorAsync("center");
        var warningMessageElement = await _page.QuerySelectorAsync<HtmlElement>("center");
        
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

        return await _retryPolicyForScrappingVideoSrc.ExecuteAsync(_ => GetVideoPlayerSourceUrl(), retryContextData);

        async Task<string?> GetVideoPlayerSourceUrl()
        {
            await _page.WaitForSelectorAsync("iframe");
            var videoIframe = await _page.QuerySelectorAsync<HtmlElement>("iframe");

            if (videoIframe is null)
            {
                throw new VideoSrcUrlNotFound();
            }

            var videoPlayerBaseUrl = await videoIframe.GetAttributeAsync<string>("src");

            return videoPlayerBaseUrl;
        }
    }

    private async Task<List<AnimeModel>> GetOtherAnimesPagePathsAsync(string animeUrl)
    {
        const string firstPartOfOtherAnimes = "Akcja";
        List<KeyValuePair<string, string>> allAnimeSeriesUrls = await GetSubmenuPagePathsByTextContent(animeUrl, firstPartOfOtherAnimes);
        const string secondPartOfOtherAnimes = "Lżejsze klimaty";
        allAnimeSeriesUrls.AddRange(await GetSubmenuPagePathsByTextContent(animeUrl, secondPartOfOtherAnimes));

        var animes = allAnimeSeriesUrls.Select(x => new AnimeModel
        {
            Title = x.Value.Trim(),
            SeriesPagesPaths = new List<string> { x.Key }
        }).ToList();

        return animes;
    }

    private async Task PopulateAnimeSeriesUrlsPathsAsync(string animeUrl, AnimeModel animeModel)
    {
        const string subMenuText = "Odcinki anime online";
        List<KeyValuePair<string, string>> allAnimeSeriesUrls = await GetSubmenuPagePathsByTextContent(animeUrl, subMenuText);

        if (!allAnimeSeriesUrls.Any())
        {
            _logger.Warning("Didn't find any anime series, for the following anime: {anime_name}", animeUrl);
        }

        _logger.Information("For anime: {anime_name}, found following anime series: {anime_series}", animeUrl, string.Join(", ", allAnimeSeriesUrls));

        animeModel.SeriesPagesPaths = allAnimeSeriesUrls.Select(x => x.Key).ToList();
    }

    private async Task<List<KeyValuePair<string, string>>> GetSubmenuPagePathsByTextContent(string animeUrl, string subMenuText)
    {
        var subMenus = await _page.QuerySelectorAllAsync<HtmlElement>("div.pmenu_naglowek_b");
        var animeSeriesDiv = await GetFirstElementContainingText(subMenus, subMenuText);

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

        List<KeyValuePair<string, string>> allAnimeSeriesUrls = await GetSeriesUrlsFromUnorderedList(animeUrl, animeSeriesSiblingList);
        return allAnimeSeriesUrls;
    }

    private async Task<List<KeyValuePair<string, string>>> GetSeriesUrlsFromUnorderedList(string animeUrl, HtmlUnorderedListElement? animeSeriesSiblingList)
    {
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

        return allAnimeSeriesUrls;
    }

    private async Task<string> NavigateToGivenAnimePageAsync(string animeUrl)
    {
        await NavigateToPageAsync(animeUrl);

        var anchorElementForSubpageWithAnimeEpisodes = await _page.QuerySelectorAsync("center a.sub_link");
        var urlToSubpageWithEpisodes = await anchorElementForSubpageWithAnimeEpisodes.EvaluateFunctionAsync<string>("a => a.href");

        await NavigateToPageAsync(urlToSubpageWithEpisodes);

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

    private async Task NavigateToPageAsync(string url, bool skipAwait = false)
    {
        _logger.Information("Navigatin to page: {anime_web_page}", url);

        Dictionary<string, object> retryContextData = new()
        {
            { "url", url }
        };
        
        if (skipAwait)
        {
            _page.GoToAsync(url);
        } else
        {
            await _retryNavigationPolicy.ExecuteAsync(_ => _page.GoToAsync(url), retryContextData);
        }
        
    }

    public async ValueTask DisposeAsync()
    {
        _logger.Information("Dispose called");
        if (_browser is not null)
        {
            _logger.Information("Clearing browser resources");
            await _browser.DisposeAsync();
        }
            

        if (_page is not null)
        {
            _logger.Information("Clearing browser pages resources");
            await _page.DisposeAsync();
        } 
    }
}

using Wbijam.WebScrapper.Web;

public class StubWebScrapper : IWebScrapper
{
    public async Task<List<AnimeModel>> GetAnimeDataAsync()
    {
        await Task.CompletedTask;
        return new List<AnimeModel>
        {
            new AnimeModel
            {
                Title = "Anime1",
                Series = new List<AnimeSeries>
                {
                    new AnimeSeries
                    {
                        SeriesName = "Series1",
                        SeriesUrlPath = "http:// Series Url 1",
                        AnimeEpisodes = new List<AnimeEpisode>
                        {
                            new AnimeEpisode
                            {
                                EpisodeName = "Ep1",
                                EpisodePlayersUrlPath = "player url path1",
                                EpisodeReleaseDateOrRangeOfEpisodes = "1/1/2000",
                                EpisodeType = "type1",
                                EpisodeVideoUrls = new List<string>
                                {
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-R40XfMRMBUoZPth_UK_CFMa5jazno8_PLU_bbAx.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-_PLU_i3PZXb9BlAodMs9gwYypd_UK_uxxQFpEQP.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-5dFwnO9p1VB6CSBMBTt44FUwuN_UK_TbtNYNSss48Hs8nE=.html"
                                }
                            },
                            new AnimeEpisode
                            {
                                EpisodeName = "Ep2",
                                EpisodePlayersUrlPath = "player url path2",
                                EpisodeReleaseDateOrRangeOfEpisodes = "22/22/2022",
                                EpisodeType = "type2",
                                EpisodeVideoUrls = new List<string>
                                {
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-R40XfMRMBUoZPth_UK_CFMa5jazno8_PLU_bbAx.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-_PLU_i3PZXb9BlAodMs9gwYypd_UK_uxxQFpEQP.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-5dFwnO9p1VB6CSBMBTt44FUwuN_UK_TbtNYNSss48Hs8nE=.html"
                                }
                            }
                        }
                    },

                    new AnimeSeries
                    {
                        SeriesName = "Series2",
                        SeriesUrlPath = "http:// Series Url 2",
                        AnimeEpisodes = new List<AnimeEpisode>
                        {
                            new AnimeEpisode
                            {
                                EpisodeName = "Ep1",
                                EpisodePlayersUrlPath = "player url path1",
                                EpisodeReleaseDateOrRangeOfEpisodes = "1/1/2000",
                                EpisodeType = "type1",
                                EpisodeVideoUrls = new List<string>
                                {
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-R40XfMRMBUoZPth_UK_CFMa5jazno8_PLU_bbAx.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-_PLU_i3PZXb9BlAodMs9gwYypd_UK_uxxQFpEQP.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-5dFwnO9p1VB6CSBMBTt44FUwuN_UK_TbtNYNSss48Hs8nE=.html"
                                }
                            },
                            new AnimeEpisode
                            {
                                EpisodeName = "Ep2",
                                EpisodePlayersUrlPath = "player url path2",
                                EpisodeReleaseDateOrRangeOfEpisodes = "22/22/2022",
                                EpisodeType = "type2",
                                EpisodeVideoUrls = new List<string>
                                {
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-R40XfMRMBUoZPth_UK_CFMa5jazno8_PLU_bbAx.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-_PLU_i3PZXb9BlAodMs9gwYypd_UK_uxxQFpEQP.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-5dFwnO9p1VB6CSBMBTt44FUwuN_UK_TbtNYNSss48Hs8nE=.html"
                                }
                            }
                        }
                    }
                }
            },
            new AnimeModel
            {
                Title = "Anime2",
                Series = new List<AnimeSeries>
                {
                    new AnimeSeries
                    {
                        SeriesName = "Series1",
                        SeriesUrlPath = "http:// Series Url 1",
                        AnimeEpisodes = new List<AnimeEpisode>
                        {
                            new AnimeEpisode
                            {
                                EpisodeName = "Ep1",
                                EpisodePlayersUrlPath = "player url path1",
                                EpisodeReleaseDateOrRangeOfEpisodes = "1/1/2000",
                                EpisodeType = "type1",
                                EpisodeVideoUrls = new List<string>
                                {
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-R40XfMRMBUoZPth_UK_CFMa5jazno8_PLU_bbAx.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-_PLU_i3PZXb9BlAodMs9gwYypd_UK_uxxQFpEQP.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-5dFwnO9p1VB6CSBMBTt44FUwuN_UK_TbtNYNSss48Hs8nE=.html"
                                }
                            },
                            new AnimeEpisode
                            {
                                EpisodeName = "Ep2",
                                EpisodePlayersUrlPath = "player url path2",
                                EpisodeReleaseDateOrRangeOfEpisodes = "22/22/2022",
                                EpisodeType = "type2",
                                EpisodeVideoUrls = new List<string>
                                {
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-R40XfMRMBUoZPth_UK_CFMa5jazno8_PLU_bbAx.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-_PLU_i3PZXb9BlAodMs9gwYypd_UK_uxxQFpEQP.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-5dFwnO9p1VB6CSBMBTt44FUwuN_UK_TbtNYNSss48Hs8nE=.html"
                                }
                            }
                        }
                    },

                    new AnimeSeries
                    {
                        SeriesName = "Series2",
                        SeriesUrlPath = "http:// Series Url 2",
                        AnimeEpisodes = new List<AnimeEpisode>
                        {
                            new AnimeEpisode
                            {
                                EpisodeName = "Ep1",
                                EpisodePlayersUrlPath = "player url path1",
                                EpisodeReleaseDateOrRangeOfEpisodes = "1/1/2000",
                                EpisodeType = "type1",
                                EpisodeVideoUrls = new List<string>
                                {
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-R40XfMRMBUoZPth_UK_CFMa5jazno8_PLU_bbAx.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-_PLU_i3PZXb9BlAodMs9gwYypd_UK_uxxQFpEQP.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-5dFwnO9p1VB6CSBMBTt44FUwuN_UK_TbtNYNSss48Hs8nE=.html"
                                }
                            },
                            new AnimeEpisode
                            {
                                EpisodeName = "Ep2",
                                EpisodePlayersUrlPath = "player url path2",
                                EpisodeReleaseDateOrRangeOfEpisodes = "22/22/2022",
                                EpisodeType = "type2",
                                EpisodeVideoUrls = new List<string>
                                {
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-R40XfMRMBUoZPth_UK_CFMa5jazno8_PLU_bbAx.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-_PLU_i3PZXb9BlAodMs9gwYypd_UK_uxxQFpEQP.html",
                                    "https://narutoboruto.wbijam.pl/odtwarzacz-5dFwnO9p1VB6CSBMBTt44FUwuN_UK_TbtNYNSss48Hs8nE=.html"
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
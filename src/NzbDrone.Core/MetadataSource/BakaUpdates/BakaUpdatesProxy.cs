using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.BakaUpdates.Resource.SeriesSearch;
using NzbDrone.Core.MetadataSource.BakaUpdates.Result;
using NzbDrone.Core.Tv;
using RecordResource = NzbDrone.Core.MetadataSource.BakaUpdates.Resource.SeriesSearch.RecordResource;

namespace NzbDrone.Core.MetadataSource.BakaUpdates;

public class BakaUpdatesProxy : ISearchForNewSeries, IProvideSeriesInfo
{
    private readonly IHttpClient _httpClient;
    private readonly Logger _logger;
    private readonly ISeriesService _seriesService;
    private readonly IHttpRequestBuilderFactory _requestBuilder;

    public BakaUpdatesProxy(
        IHttpClient httpClient,
        Logger logger,
        ISeriesService seriesService,
        IBakaUpdatesCloudRequestBuilder requestBuilder)
    {
        _httpClient = httpClient;
        _logger = logger;
        _seriesService = seriesService;
        _requestBuilder = requestBuilder.Services;
    }

    public List<Series> SearchForNewSeries(string title)
    {
        var httpRequest = _requestBuilder.Create()
            .Resource("series/search")
            .AddBodyProperty("search", title)
            .AddBodyProperty("page", 1)
            .AddBodyProperty("perpage", 100)
            .Post()
            .Build();

        var httpResponse = _httpClient.Post<SeriesSearchResult>(httpRequest);

        return httpResponse.Resource.Results.Where(Filter).SelectList(MapSearchResult);
    }

    private bool Filter(SeriesSearchResultResource resource)
    {
        if (resource.Record.Type.EqualsIgnoreCase("doujinshi"))
        {
            return false;
        }

        if (resource.Record.Type.EqualsIgnoreCase("novel"))
        {
            return false;
        }

        _logger.Warn("Found type: {Type}", resource.Record.Type);

        foreach (var genreResource in resource.Record.Genres)
        {
            if (genreResource.Genre.EqualsIgnoreCase("adult"))
            {
                return false;
            }

            _logger.Warn("Found genre: {Genre}", genreResource.Genre);
        }

        return true;
    }

    private Series MapSearchResult(SeriesSearchResultResource resource)
    {
        var series = _seriesService.FindByTvdbId(resource.Record.SeriesId);

        if (series == null)
        {
            series = MapSeries(resource.Record);
        }

        return series;
    }

    private Series MapSeries(RecordResource resource)
    {
        var series = new Series();
        series.TvdbId = resource.SeriesId;

        // MANGARR TODO: Should this be added?
        // series.BakaUpdatesSlug = Parser.Parser.ParseBakaUpdatesSlug(resource.Url);

        series.Title = resource.Title;
        series.CleanTitle = Parser.Parser.CleanSeriesTitle(resource.Title);
        series.SortTitle = SeriesTitleNormalizer.Normalize(resource.Title, -1);

        if (resource.LastUpdated != null)
        {
            series.LastAired = DateTimeOffset.FromUnixTimeSeconds(resource.LastUpdated.Timestamp).UtcDateTime;
        }

        series.Overview = resource.Description;

        series.TitleSlug = Parser.Parser.ParseBakaUpdatesTitleSlug(resource.Url);
        series.Ratings = MapRatings(resource);
        series.Genres = MapGenres(resource);
        series.Seasons = new List<Season>();
        series.Images = MapImages(resource);
        series.Monitored = true;

        return series;
    }

    private Ratings MapRatings(RecordResource resource)
    {
        return new Ratings()
        {
            Value = resource.BayesianRating,
            Votes = resource.RatingVotes
        };
    }

    private List<string> MapGenres(RecordResource resource)
    {
        return resource.Genres?.SelectList(x => x.Genre) ?? new List<string>();
    }

    private List<MediaCover.MediaCover> MapImages(RecordResource resource)
    {
        return new List<MediaCover.MediaCover>
        {
            new MediaCover.MediaCover(MediaCoverTypes.Poster, resource.Image.Url.Original)
        };
    }

    public Tuple<Series, List<Episode>> GetSeriesInfo(int tvdbSeriesId)
    {
        throw new NotImplementedException("Use GetSeriesInfo(long bakaUpdatesId) instead.");
    }

    public Tuple<Series, List<Episode>> GetSeriesInfo(long bakaUpdatesId, bool skipChapterFetching)
    {
        var extraMangaInfo = GetExtraMangaInfo(bakaUpdatesId);
        if (skipChapterFetching)
        {
            return new Tuple<Series, List<Episode>>(extraMangaInfo, new List<Episode>());
        }

        var releaseSearchResults = SearchReleases(bakaUpdatesId);
        var chapters = MapToChapters(releaseSearchResults);
        UpdateVolumes(extraMangaInfo, chapters);

        return new Tuple<Series, List<Episode>>(extraMangaInfo, chapters);
    }

    private Series GetExtraMangaInfo(long bakaUpdatesId)
    {
        var httpRequest = _requestBuilder.Create()
            .Resource($"series/{bakaUpdatesId}")
            .Build();

        var httpResponse = _httpClient.Get<SeriesGetResult>(httpRequest);

        if (httpResponse.HasHttpError)
        {
            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                throw new SeriesNotFoundException(bakaUpdatesId);
            }
            else
            {
                throw new HttpException(httpRequest, httpResponse);
            }
        }

        return MapManga(httpResponse.Resource);
    }

    private Series MapManga(SeriesGetResult resource)
    {
        return new Series()
        {
            TvdbId = resource.SeriesId,

            // MANGARR TODO: Should this be added?
            // BakaUpdatesSlug = Parser.Parser.ParseBakaUpdatesSlug(resource.Url),
            Title = resource.Title,
            TitleSlug = Parser.Parser.ParseBakaUpdatesTitleSlug(resource.Url),
            CleanTitle = Parser.Parser.CleanSeriesTitle(resource.Title),
            SortTitle = Parser.Parser.NormalizeTitle(resource.Title),
            LastAired = DateTimeOffset.FromUnixTimeSeconds(resource.LastUpdated.Timestamp).UtcDateTime,
            Year = resource.Year.ParseInt32() ?? 0,
            Overview = resource.Description,
            Ratings = MapRatings(resource),
            Genres = resource.Genres?.SelectList(x => x.Genre) ?? new List<string>(),
            Images = MapImages(resource),
            Monitored = true,
            AlternateTitles = resource.Associated.SelectList(x => x.Title),

            // [MANGARR] TODO: Add all these again
            // Anime = MapAnime(resource),
            // Authors = MapAuthors(resource),
            // Categories = MapCategories(resource),
            // CategoryRecommendations = MapCategoryRecommendations(resource),
            // Publications = MapPublications(resource),
            // Publishers = MapPublishers(resource),
            // Recommendations = MapRecommendations(resource),
            // Related = MapRelated(resource)
        };
    }

    private static List<MediaCover.MediaCover> MapImages(SeriesGetResult resource)
    {
        return new List<MediaCover.MediaCover>
        {
            new MediaCover.MediaCover(MediaCoverTypes.Poster, resource.Image.Url.Original)
        };
    }

    private static Ratings MapRatings(SeriesGetResult resource)
    {
        return new Ratings()
        {
            Value = resource.BayesianRating,
            Votes = resource.RatingVotes
        };
    }

    // [MANGARR] TODO: Add all these again
    // private Anime MapAnime(SeriesGetResult resource)
    // {
    //     return new Anime()
    //     {
    //         Start = resource.Anime.Start,
    //         End = resource.Anime.End,
    //     };
    // }
    //
    // private List<Author> MapAuthors(SeriesGetResult resource)
    // {
    //     return resource.Authors.SelectList(
    //         x => new Author()
    //         {
    //             Name = x.Name,
    //             Id = x.AuthorId,
    //             Type = x.Type
    //         });
    // }
    //
    // private List<Category> MapCategories(SeriesGetResult resource)
    // {
    //     return resource.Categories.SelectList(
    //         x => new Category()
    //         {
    //             Name = x.Category,
    //             Votes = x.Votes,
    //             VotesPlus = x.VotesPlus,
    //             VotesMinus = x.VotesMinus,
    //             AddedBy = x.AddedBy
    //         });
    // }
    //
    // private List<CategoryRecommendation> MapCategoryRecommendations(SeriesGetResult resource)
    // {
    //     return resource.CategoryRecommendations.SelectList(
    //         x => new CategoryRecommendation()
    //         {
    //             Name = x.SeriesName,
    //             Id = x.SeriesId,
    //             Weight = x.Weight
    //         });
    // }
    //
    // private List<Publication> MapPublications(SeriesGetResult resource)
    // {
    //     return resource.Publications.SelectList(
    //         x => new Publication()
    //         {
    //             Name = x.PublicationName,
    //             Publisher = x.PublisherName,
    //             PublisherId = x.PublisherId
    //         });
    // }
    //
    // private List<Publisher> MapPublishers(SeriesGetResult resource)
    // {
    //     return resource.Publishers.SelectList(
    //         x => new Publisher()
    //         {
    //             Name = x.PublisherName,
    //             Id = x.PublisherId,
    //             Type = x.Type,
    //             Notes = x.Notes
    //         });
    // }
    //
    // private List<Recommendation> MapRecommendations(SeriesGetResult resource)
    // {
    //     return resource.Recommendations.SelectList(
    //         x => new Recommendation()
    //         {
    //             Name = x.SeriesName,
    //             Id = x.SeriesId,
    //             Weight = x.Weight
    //         });
    // }
    //
    // private List<Related> MapRelated(SeriesGetResult resource)
    // {
    //     return resource.RelatedSeries.SelectList(
    //         x => new Related()
    //         {
    //             Name = x.RelatedSeriesName,
    //             Id = x.RelationId,
    //             SeriesId = x.RelatedSeriesId,
    //             Type = x.RelationType,
    //             TriggeredByRelationId = x.TriggeredByRelationId
    //         });
    // }

    private List<ReleaseSearchResult> SearchReleases(long bakaUpdatesMangaId)
    {
        var initialResult = SearchReleases(bakaUpdatesMangaId, 1);

        if (initialResult == null)
        {
            return new List<ReleaseSearchResult>();
        }

        var results = new List<ReleaseSearchResult>()
        {
            initialResult
        };

        var totalPages = Math.Ceiling((float)initialResult.TotalHits / initialResult.PerPage);
        for (var i = 2; i <= totalPages; i++)
        {
            results.Add(SearchReleases(bakaUpdatesMangaId, i));
        }

        return results;
    }

    private ReleaseSearchResult SearchReleases(long bakaUpdatesMangaId, int page)
    {
        var httpRequest = _requestBuilder.Create()
            .Post()
            .AddBodyProperty("search", bakaUpdatesMangaId.ToString())
            .AddBodyProperty("search_type", "series")
            .AddBodyProperty("page", page)
            .AddBodyProperty("perpage", 100)
            .Resource("releases/search")
            .Build();

        var httpResponse = _httpClient.Post<ReleaseSearchResult>(httpRequest);

        if (httpResponse.HasHttpError)
        {
            throw new HttpException(httpRequest, httpResponse);
        }

        return httpResponse.Resource;
    }

    private List<Episode> MapToChapters(List<ReleaseSearchResult> results)
    {
        var records = results.SelectMany(x => x.Results)
            .Select(x => x.Record)
            .OrderBy(x => x.Id)
            .ThenBy(x => x.Chapter)
            .ThenBy(x => x.Volume)
            .ToList();

        if (!records.Any())
        {
            return new List<Episode>();
        }

        var chapters = new Dictionary<int, Episode>();

        foreach (var record in records)
        {
            if (int.TryParse(record.Chapter, out var chapterNumber))
            {
                MapChapter(record, chapterNumber);
            }
            else if (record.Chapter.Contains('-'))
            {
                var splits = record.Chapter.Split('-', StringSplitOptions.TrimEntries);
                if (splits.Length != 2)
                {
                    _logger.Warn(
                        "Unable to process chapter Id: {Id}, Number: {Number}, Title: {Title}",
                        record.Id,
                        record.Chapter,
                        record.Title);

                    continue;
                }

                var from = splits[0].ParseInt32() ?? -1;
                var to = splits[1].ParseInt32() ?? -1;

                if (from == -1 || to == -1)
                {
                    _logger.Warn(
                        "Unable to process chapter Id: {Id}, Number: {Number}, Title: {Title}",
                        record.Id,
                        record.Chapter,
                        record.Title);

                    continue;
                }

                for (var i = from; i <= to; i++)
                {
                    MapChapter(record, i);
                }
            }
            else
            {
                _logger.Warn(
                    "Unable to process chapter Id: {Id}, Number: {Number}, Title: {Title}",
                    record.Id,
                    record.Chapter,
                    record.Title);
            }
        }

        return chapters.SelectList(x => x.Value);

        void MapChapter(Resource.ReleaseSearch.RecordResource record, int chapterNumber)
        {
            var chapter = MapToChapterWithNumber(record, chapterNumber);

            if (chapters.TryGetValue(chapterNumber, out var existingChapter))
            {
                chapters[chapterNumber] = MergeChapters(existingChapter, chapter);
            }
            else
            {
                chapters.Add(chapterNumber, chapter);
            }
        }
    }

    private Episode MapToChapterWithNumber(Resource.ReleaseSearch.RecordResource record, int chapterNumber)
    {
        var chapter = new Episode
        {
            TvdbId = record.Id,
            EpisodeNumber = chapterNumber,
            Title = "Chapter " + chapterNumber,

            // SeasonNumber = record.Volume.ParseInt32() ?? 1,
            SeasonNumber = 1,
            AirDate = record.ReleaseDate,
            AirDateUtc = DateTime.ParseExact(record.ReleaseDate, Episode.AIR_DATE_FORMAT, CultureInfo.InvariantCulture),

            // MANGARR TODO: Add this again
            // Groups = record.Groups.SelectList(MapGroup)
        };

        return chapter;
    }

    // MANGARR TODO: Add this again
    // private Group MapGroup(GroupResource resource)
    // {
    //     return new Group()
    //     {
    //         BakaUpdatesId = resource.GroupId,
    //         Name = resource.Name
    //     };
    // }

    private Episode MergeChapters(Episode existing, Episode newChapter)
    {
        var clone = existing.JsonClone();

        if (existing.SeasonNumber != newChapter.SeasonNumber)
        {
            if (newChapter.SeasonNumber != 0)
            {
                clone.SeasonNumber = newChapter.SeasonNumber;
            }
        }

        // MANGARR TODO: Add this again
        // clone.AdditionalBakaUpdatesIds.Add(newChapter.BakaUpdatesId);
        // clone.AdditionalBakaUpdatesIds.Sort();
        // clone.Groups.AddRange(newChapter.Groups);

        // MANGARR TODO: Dedupe groups

        return clone;
    }

    private void UpdateVolumes(Series extraMangaInfo, List<Episode> chapters)
    {
        var volumeNumbers = chapters.SelectList(x => x.SeasonNumber);

        if (volumeNumbers.Any())
        {
            extraMangaInfo.Seasons = volumeNumbers.Distinct().SelectList(x => new Season() { SeasonNumber = x });
        }
    }

    List<Series> ISearchForNewSeries.SearchForNewSeriesByImdbId(string imdbId)
    {
        throw new NotImplementedException();
    }

    List<Series> ISearchForNewSeries.SearchForNewSeriesByAniListId(int aniListId)
    {
        throw new NotImplementedException();
    }

    List<Series> ISearchForNewSeries.SearchForNewSeriesByTmdbId(int tmdbId)
    {
        throw new NotImplementedException();
    }

    List<Series> ISearchForNewSeries.SearchForNewSeriesByMyAnimeListId(int malId)
    {
        throw new NotImplementedException();
    }
}

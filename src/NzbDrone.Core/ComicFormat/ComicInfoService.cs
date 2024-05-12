using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Download.Clients.FakeTorrent;

namespace NzbDrone.Core.ComicFormat;

public interface IComicInfoService
{
    public ComicInfo CreateComicInfo(FakeTorrentDownloadInfo downloadInfo, bool hasCover);
}

public class ComicInfoService : IComicInfoService
{
    public ComicInfo CreateComicInfo(FakeTorrentDownloadInfo downloadInfo, bool hasCover)
    {
        return new ComicInfo()
        {
            Series = downloadInfo.Episode.Series.Title,
            Title = downloadInfo.Episode.Series.Title + " - Chapter " +
                    downloadInfo.Episode.Episodes.First().EpisodeNumber,
            Summary = downloadInfo.Episode.Series.Overview,
            PageCount = downloadInfo.TotalPages,
            Web = "https://mangaupdates.com/series/" + downloadInfo.Episode.Series.TvdbId,
            Genres = string.Join(',', downloadInfo.Episode.Series.Genres),
            Pages = CreatePages(downloadInfo, hasCover)
        };
    }

    private List<ComicInfoPage> CreatePages(FakeTorrentDownloadInfo downloadInfo, bool hasCover)
    {
        var pages = new List<ComicInfoPage>();

        if (hasCover)
        {
            pages.Add(new ComicInfoPage()
            {
                Image = 0,
                Type = ComicPageType.FrontCover
            });
        }

        for (var i = 0; i < downloadInfo.TotalPages; i++)
        {
            pages.Add(new ComicInfoPage()
            {
                Image = i + 1,
                Type = ComicPageType.Story
            });
        }

        return pages;
    }
}

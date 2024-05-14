using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download.Clients.FakeTorrent;

public class FakeTorrent : TorrentClientBase<FakeTorrentSettings>
{
    private readonly IFakeTorrentDownloadQueue _downloadQueue;
    private readonly IFakeTorrentDownloader _downloader;
    private readonly string _torrentsFolder;
    private readonly string _destinationFolder;

    public FakeTorrent(ITorrentFileInfoReader torrentFileInfoReader,
        IHttpClient httpClient,
        IConfigService configService,
        IDiskProvider diskProvider,
        IRemotePathMappingService remotePathMappingService,
        ILocalizationService localizationService,
        IBlocklistService blocklistService,
        Logger logger,
        IFakeTorrentDownloadQueue downloadQueue,
        IFakeTorrentDownloader downloader,
        IAppFolderInfo appFolderInfo)
        : base(torrentFileInfoReader,
            httpClient,
            configService,
            diskProvider,
            remotePathMappingService,
            localizationService,
            blocklistService,
            logger)
    {
        _downloadQueue = downloadQueue;
        _downloader = downloader;

        _torrentsFolder = Path.Combine(appFolderInfo.TempFolder, "FakeTorrent", "Torrents");
        _destinationFolder = Path.Combine(appFolderInfo.TempFolder, "FakeTorrent", "Completed");
        _diskProvider.EnsureFolder(_torrentsFolder);
        _diskProvider.EnsureFolder(_destinationFolder);
    }

    public override string Name => "Fake Torrent";
    public override bool PreferTorrentFile => true;

    public override IEnumerable<DownloadClientItem> GetItems()
    {
        var items = new List<DownloadClientItem>();
        var totalSecondsToWait = 0;

        foreach (var download in _downloadQueue.Downloads)
        {
            var remainingSize = download.TotalPages - download.PagesDownloaded;
            totalSecondsToWait += remainingSize;

            items.Add(new DownloadClientItem()
            {
                OutputPath = download.Status == DownloadItemStatus.Completed ? new OsPath(download.CbzPath) : default,
                DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this, false),
                DownloadId = FakeTorrentDownloadIdGenerator.Generate(download.Episode),
                Status = download.Status,
                TotalSize = download.TotalPages,
                RemainingSize = remainingSize,
                RemainingTime = TimeSpan.FromSeconds(totalSecondsToWait),
                Title = download.Episode.ParsedEpisodeInfo.ReleaseTitle,
                CanMoveFiles = download.Status == DownloadItemStatus.Completed,
                CanBeRemoved = download.Status == DownloadItemStatus.Completed
            });
        }

        return items;
    }

    public override void RemoveItem(DownloadClientItem item, bool deleteData)
    {
        _downloadQueue.Remove(item, deleteData);
    }

    public override DownloadClientInfo GetStatus()
    {
        return new DownloadClientInfo()
        {
            IsLocalhost = true,
            RemovesCompletedDownloads = false,
            OutputRootFolders = new List<OsPath>()
            {
                new OsPath(_destinationFolder)
            }
        };
    }

    public override async Task<string> Download(RemoteEpisode remoteEpisode, IIndexer indexer)
    {
        var download = await base.Download(remoteEpisode, indexer);
        return FakeTorrentDownloadIdGenerator.Generate(remoteEpisode);
    }

    protected override string AddFromMagnetLink(RemoteEpisode remoteEpisode, string hash, string magnetLink)
    {
        throw new NotSupportedException();
    }

    protected override string AddFromTorrentFile(RemoteEpisode remoteEpisode, string hash, string filename, byte[] fileContent)
    {
        var title = remoteEpisode.Release.Title;

        title = FileNameBuilder.CleanFileName(title);

        var filepath = Path.Combine(_torrentsFolder, string.Format("{0}.torrent", title));

        using (var stream = _diskProvider.OpenWriteStream(filepath))
        {
            stream.Write(fileContent, 0, fileContent.Length);
        }

        _downloadQueue.Enqueue(remoteEpisode, filepath, Path.Combine(_destinationFolder, title + ".cbz"));
        _downloader.Start();

        return null;
    }

    protected override void Test(List<ValidationFailure> failures)
    {
        failures.AddIfNotNull(TestFolder(_torrentsFolder, "TorrentsFolder"));
        failures.AddIfNotNull(TestFolder(_destinationFolder, "DestinationFolder"));
    }
}

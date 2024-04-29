using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.Download.Clients.FakeTorrent;

public interface IFakeTorrentDownloader
{
    void Start();
    void Stop();
}

public class FakeTorrentDownloader : IFakeTorrentDownloader
{
    private readonly IHttpClient _httpClient;
    private readonly ITorrentFilePathsReader _torrentFilePathsReader;
    private readonly IFakeTorrentDownloadQueue _downloadQueue;
    private readonly IDiskProvider _diskProvider;
    private readonly IArchiveService _archiveService;
    private readonly IAppFolderInfo _appFolderInfo;

    private CancellationTokenSource _cancellationTokenSource;

    public FakeTorrentDownloader(IHttpClient httpClient, ITorrentFilePathsReader torrentFilePathsReader, IFakeTorrentDownloadQueue downloadQueue, IDiskProvider diskProvider, IArchiveService archiveService, IAppFolderInfo appFolderInfo)
    {
        _httpClient = httpClient;
        _torrentFilePathsReader = torrentFilePathsReader;
        _downloadQueue = downloadQueue;
        _diskProvider = diskProvider;
        _archiveService = archiveService;
        _appFolderInfo = appFolderInfo;
    }

    private async Task DownloadTask(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (!_downloadQueue.Dequeue(out var current))
            {
                await Task.Delay(1000, ct);
                continue;
            }

            current.Status = DownloadItemStatus.Downloading;
            var filePathsFromTorrent = _torrentFilePathsReader.GetFilePathsFromTorrent(current.TorrentPath).ToList();
            current.TotalPages = filePathsFromTorrent.Count;

            var downloadFolder = Path.Combine(_appFolderInfo.TempFolder,
                "FakeTorrent",
                "Downloads",
                FileNameBuilder.CleanFileName(current.Episode.ParsedEpisodeInfo.ReleaseTitle));

            _diskProvider.EnsureFolder(downloadFolder);

            var pages = new List<string>();

            for (var i = 0; i < filePathsFromTorrent.Count; i++)
            {
                var url = filePathsFromTorrent[i];
                var downloadPath = Path.Combine(downloadFolder, "Page_" + i + ".png");
                await _httpClient.DownloadFileAsync(url, downloadPath).ConfigureAwait(false);
                pages.Add(downloadPath);
                current.PagesDownloaded++;
                await Task.Delay(100, ct).ConfigureAwait(false);
            }

            _archiveService.CreateZip(current.CbzPath, _diskProvider.GetFiles(downloadFolder, false));
            _downloadQueue.Complete(current);
            _diskProvider.DeleteFolder(downloadFolder, true);

            current.Status = DownloadItemStatus.Completed;
        }
    }

    public void Start()
    {
        if (_cancellationTokenSource != null)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => DownloadTask(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    public void Stop()
    {
        if (_cancellationTokenSource == null)
        {
            return;
        }

        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Clients.FakeTorrent;

public interface IFakeTorrentDownloadQueue
{
    FakeTorrentDownloadInfo Current { get; }
    IEnumerable<FakeTorrentDownloadInfo> Downloads { get; }

    void Enqueue(RemoteEpisode remoteEpisode, string torrentPath, string cbzPath);
    bool Dequeue(out FakeTorrentDownloadInfo downloadInfo);
    void Complete(FakeTorrentDownloadInfo downloadInfo);
    void Remove(DownloadClientItem downloadItem, bool deleteData);
}

public class FakeTorrentDownloadQueue : IFakeTorrentDownloadQueue
{
    private readonly List<FakeTorrentDownloadInfo> _queue = new List<FakeTorrentDownloadInfo>();
    private readonly List<FakeTorrentDownloadInfo> _downloaded = new List<FakeTorrentDownloadInfo>();
    private readonly IDiskProvider _diskProvider;

    public FakeTorrentDownloadQueue(IDiskProvider diskProvider)
    {
        _diskProvider = diskProvider;
    }

    public FakeTorrentDownloadInfo Current { get; private set; }

    public IEnumerable<FakeTorrentDownloadInfo> Downloads
    {
        get
        {
            foreach (var info in _downloaded)
            {
                yield return info;
            }

            if (Current != null)
            {
                yield return Current;
            }

            foreach (var info in _queue)
            {
                yield return info;
            }
        }
    }

    public void Enqueue(RemoteEpisode remoteEpisode, string torrentPath, string cbzPath)
    {
        var info = new FakeTorrentDownloadInfo()
        {
            Episode = remoteEpisode,
            PagesDownloaded = 0,
            TotalPages = 100,
            Status = DownloadItemStatus.Queued,
            TorrentPath = torrentPath,
            CbzPath = cbzPath,
        };

        _queue.Add(info);
    }

    public bool Dequeue(out FakeTorrentDownloadInfo downloadInfo)
    {
        if (!_queue.Any())
        {
            downloadInfo = default;
            return false;
        }

        downloadInfo = _queue.First();
        _queue.RemoveAt(0);
        Current = downloadInfo;
        return true;
    }

    public void Complete(FakeTorrentDownloadInfo downloadInfo)
    {
        if (Current != downloadInfo)
        {
            throw new InvalidOperationException("Uhhh oh");
        }

        _downloaded.Add(Current);
        Current = null;
    }

    public void Remove(DownloadClientItem downloadItem, bool deleteData)
    {
        for (var i = 0; i < _downloaded.Count; i++)
        {
            var info = _downloaded[i];
            var id = FakeTorrentDownloadIdGenerator.Generate(info.Episode);
            if (id == downloadItem.DownloadId)
            {
                _diskProvider.DeleteFile(info.TorrentPath);
                _downloaded.RemoveAt(i);
                return;
            }
        }

        if (Current != null)
        {
            var currentId = FakeTorrentDownloadIdGenerator.Generate(Current.Episode);
            if (currentId == downloadItem.DownloadId)
            {
                _diskProvider.DeleteFile(Current.TorrentPath);
                Current = null;
                return;
            }
        }

        for (var i = 0; i < _queue.Count; i++)
        {
            var info = _queue[i];
            var id = FakeTorrentDownloadIdGenerator.Generate(info.Episode);
            if (id == downloadItem.DownloadId)
            {
                _diskProvider.DeleteFile(info.TorrentPath);
                _queue.RemoveAt(i);
                return;
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using MonoTorrent;
using NzbDrone.Common.Disk;

namespace NzbDrone.Core.MediaFiles.TorrentInfo;

public interface ITorrentFilePathsReader
{
    IEnumerable<string> GetFilePathsFromTorrent(string torrentPath);
}

public class TorrentFilePathsReader : ITorrentFilePathsReader
{
    private readonly IDiskProvider _diskProvider;

    public TorrentFilePathsReader(IDiskProvider diskProvider)
    {
        _diskProvider = diskProvider;
    }

    public IEnumerable<string> GetFilePathsFromTorrent(string torrentPath)
    {
        Torrent torrent;

        using (var stream = _diskProvider.OpenReadStream(torrentPath))
        {
            torrent = Torrent.Load(stream);
        }

        return torrent.Files.Select(x => x.Path);
    }
}

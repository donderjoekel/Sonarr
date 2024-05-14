using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BencodeNET.Torrents;
using NLog;
using NzbDrone.Common.Disk;

namespace NzbDrone.Core.MediaFiles.TorrentInfo;

public interface ITorrentFilePathsReader
{
    IEnumerable<string> GetFilePathsFromTorrent(string torrentPath);
}

public class TorrentFilePathsReader : ITorrentFilePathsReader
{
    private readonly IDiskProvider _diskProvider;
    private readonly Logger _logger;

    public TorrentFilePathsReader(IDiskProvider diskProvider, Logger logger)
    {
        _diskProvider = diskProvider;
        _logger = logger;
    }

    public IEnumerable<string> GetFilePathsFromTorrent(string torrentPath)
    {
        Torrent torrent;

        using (var stream = _diskProvider.OpenReadStream(torrentPath))
        {
            try
            {
                torrent = new TorrentParser().Parse(stream);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to parse torrent file: {Path}", torrentPath);
                return Array.Empty<string>();
            }
        }

        return torrent.Files.Select(x => Encoding.Default.GetString(Convert.FromBase64String(x.FullPath)));
    }
}

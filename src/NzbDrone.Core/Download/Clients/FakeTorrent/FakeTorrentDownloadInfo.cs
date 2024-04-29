using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Clients.FakeTorrent;

public class FakeTorrentDownloadInfo
{
    public RemoteEpisode Episode { get; set; }
    public int TotalPages { get; set; }
    public int PagesDownloaded { get; set; }
    public DownloadItemStatus Status { get; set; }
    public string TorrentPath { get; set; }
    public string CbzPath { get; set; }
}

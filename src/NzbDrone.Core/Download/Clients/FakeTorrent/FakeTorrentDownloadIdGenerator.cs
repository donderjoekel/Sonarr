using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Clients.FakeTorrent;

public class FakeTorrentDownloadIdGenerator
{
    public static string Generate(RemoteEpisode remoteEpisode)
    {
        return "FakeTorrent:" + remoteEpisode.ParsedEpisodeInfo.ReleaseTitle;
    }
}

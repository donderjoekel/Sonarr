using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Organizer
{
    public class NamingConfig : ModelBase
    {
        public static NamingConfig Default => new NamingConfig
        {
            RenameEpisodes = true,
            ReplaceIllegalCharacters = true,
            ColonReplacementFormat = ColonReplacementFormat.Smart,
            MultiEpisodeStyle = MultiEpisodeStyle.PrefixedRange,
            StandardEpisodeFormat = "{Series Title} - Chapter {episode:00}",
            DailyEpisodeFormat = "{Series Title} - {Air-Date} - {Episode Title} {Quality Full}",
            AnimeEpisodeFormat = "{Series Title} - S{season:00}E{episode:00} - {Episode Title} {Quality Full}",
            SeriesFolderFormat = "{Series Title}",
            SeasonFolderFormat = "Volume {season}",
            SpecialsFolderFormat = "Specials"
        };

        public bool RenameEpisodes { get; set; }
        public bool ReplaceIllegalCharacters { get; set; }
        public ColonReplacementFormat ColonReplacementFormat { get; set; }
        public MultiEpisodeStyle MultiEpisodeStyle { get; set; }
        public string StandardEpisodeFormat { get; set; }
        public string DailyEpisodeFormat { get; set; }
        public string AnimeEpisodeFormat { get; set; }
        public string SeriesFolderFormat { get; set; }
        public string SeasonFolderFormat { get; set; }
        public string SpecialsFolderFormat { get; set; }
    }
}

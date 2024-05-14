namespace NzbDrone.Core.ComicFormat;

public class ComicInfoPage
{
    public int Image { get; set; }
    public ComicPageType Type { get; set; } = ComicPageType.Story;
    public bool DoublePage { get; set; }
    public long ImageSize { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Bookmark { get; set; } = string.Empty;
    public int ImageWidth { get; set; } = -1;
    public int ImageHeight { get; set; } = -1;
}

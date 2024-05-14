using System.Collections.Generic;
using NzbDrone.Core.MetadataSource.BakaUpdates.Resource.ReleaseSearch;

namespace NzbDrone.Core.MetadataSource.BakaUpdates.Result;

public class ReleaseSearchResult
{
    public int TotalHits { get; set; }
    public int Page { get; set; }
    public int PerPage { get; set; }
    public List<ReleaseSearchResultResource> Results { get; set; }
}

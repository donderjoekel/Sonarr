using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.ImportLists.Exclusions
{
    public class ImportListExclusion : ModelBase
    {
        public long TvdbId { get; set; }
        public string Title { get; set; }
    }
}

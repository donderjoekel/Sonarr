using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Exceptions
{
    public class SeriesNotFoundException : NzbDroneException
    {
        public long TvdbSeriesId { get; set; }

        public SeriesNotFoundException(long tvdbSeriesId)
            : base(string.Format("Series with tvdbid {0} was not found, it may have been removed from TheTVDB.", tvdbSeriesId))
        {
            TvdbSeriesId = tvdbSeriesId;
        }

        public SeriesNotFoundException(long tvdbSeriesId, string message, params object[] args)
            : base(message, args)
        {
            TvdbSeriesId = tvdbSeriesId;
        }

        public SeriesNotFoundException(long tvdbSeriesId, string message)
            : base(message)
        {
            TvdbSeriesId = tvdbSeriesId;
        }
    }
}

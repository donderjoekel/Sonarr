﻿namespace NzbDrone.Core.MetadataSource.BakaUpdates.Resource.SeriesGet;

public class RecommendationResource
{
    public string SeriesName { get; set; }
    public long SeriesId { get; set; }
    public int Weight { get; set; }
}

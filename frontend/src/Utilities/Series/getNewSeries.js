
function getNewSeries(series, payload) {
  const {
    rootFolderPath,
    monitor,
    monitorNewItems,
    qualityProfileId,
    seriesType,
    seasonFolder,
    useAlternateTitlesForSearch,
    tags,
    searchForMissingEpisodes = false,
    searchForCutoffUnmetEpisodes = false
  } = payload;

  const addOptions = {
    monitor,
    searchForMissingEpisodes,
    searchForCutoffUnmetEpisodes
  };

  series.addOptions = addOptions;
  series.monitored = true;
  series.monitorNewItems = monitorNewItems;
  series.qualityProfileId = qualityProfileId;
  series.rootFolderPath = rootFolderPath;
  series.seriesType = seriesType;
  series.seasonFolder = seasonFolder;
  series.useAlternateTitlesForSearch = useAlternateTitlesForSearch;
  series.tags = tags;

  return series;
}

export default getNewSeries;

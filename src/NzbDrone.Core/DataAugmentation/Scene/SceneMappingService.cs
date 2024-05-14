using System.Collections.Generic;

namespace NzbDrone.Core.DataAugmentation.Scene
{
    public interface ISceneMappingService
    {
        List<string> GetSceneNames(long tvdbId, List<int> seasonNumbers, List<int> sceneSeasonNumbers);
        long? FindTvdbId(string sceneTitle, string releaseTitle, int sceneSeasonNumber);
        List<SceneMapping> FindByTvdbId(long tvdbId);
        SceneMapping FindSceneMapping(string sceneTitle, string releaseTitle, int sceneSeasonNumber);
        int? GetSceneSeasonNumber(string seriesTitle, string releaseTitle);
    }

    public class SceneMappingService : ISceneMappingService
    {
        public List<string> GetSceneNames(long tvdbId, List<int> seasonNumbers, List<int> sceneSeasonNumbers)
        {
            return new List<string>();
        }

        public long? FindTvdbId(string seriesTitle, string releaseTitle, int sceneSeasonNumber)
        {
            return FindSceneMapping(seriesTitle, releaseTitle, sceneSeasonNumber)?.TvdbId;
        }

        public List<SceneMapping> FindByTvdbId(long tvdbId)
        {
            return new List<SceneMapping>();
        }

        public SceneMapping FindSceneMapping(string seriesTitle, string releaseTitle, int sceneSeasonNumber)
        {
            return null;
        }

        public int? GetSceneSeasonNumber(string seriesTitle, string releaseTitle)
        {
            return FindSceneMapping(seriesTitle, releaseTitle, -1)?.SceneSeasonNumber;
        }
    }
}

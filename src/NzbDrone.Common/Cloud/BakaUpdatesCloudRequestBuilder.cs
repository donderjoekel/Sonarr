using NzbDrone.Common.Http;

namespace NzbDrone.Common.Cloud;

public interface IBakaUpdatesCloudRequestBuilder
{
    IHttpRequestBuilderFactory Services { get; }
}

public class BakaUpdatesCloudRequestBuilder : IBakaUpdatesCloudRequestBuilder
{
    public BakaUpdatesCloudRequestBuilder()
    {
        Services = new HttpRequestBuilder("https://api.mangaupdates.com/v1/")
            .SetHeader("Content-Type", "application/json")
            .SetHeader("Accept", "*/*")
            .SetHeader("Accept-Encoding", "gzip, deflate, br")
            .SetHeader("Connection", "keep-alive")
            .SetHeader("Host", "api.mangaupdates.com")
            .CreateFactory();
    }

    public IHttpRequestBuilderFactory Services { get; }
}

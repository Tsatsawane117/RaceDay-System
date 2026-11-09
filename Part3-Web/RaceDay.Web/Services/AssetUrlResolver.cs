namespace RaceDay.Web.Services;

public interface IAssetUrlResolver
{

    string? Resolve(string? url);
}

public class AssetUrlResolver : IAssetUrlResolver
{
    private readonly string _publicBaseUrl;

    public AssetUrlResolver(IConfiguration configuration)
    {
        _publicBaseUrl = (configuration["ApiSettings:PublicBaseUrl"] ?? configuration["ApiSettings:BaseUrl"] ?? string.Empty).TrimEnd('/');
    }

    public string? Resolve(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : _publicBaseUrl + url;
    }
}

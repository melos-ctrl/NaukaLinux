using System.Text.RegularExpressions;

namespace Backend.Validation;


public static partial class VideoUrlValidator
{
    private static readonly HashSet<string> YoutubeHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com", "www.youtube.com", "m.youtube.com",
        "youtube-nocookie.com", "www.youtube-nocookie.com",
        "youtu.be"
    };

    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
    private static partial Regex VideoIdRegex();

    public static bool TryNormalize(string? url, out string? embedUrl)
    {
        embedUrl = null;
        if (string.IsNullOrWhiteSpace(url)) return true;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return false;
        if (!YoutubeHosts.Contains(uri.Host)) return false;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? videoId = null;

        if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            videoId = segments.FirstOrDefault();
        }
        else if (segments.Length >= 2 && segments[0] is "embed" or "shorts" or "live" or "v")
        {
            videoId = segments[1];
        }
        else if (segments.Length == 1 && segments[0] == "watch")
        {
            videoId = System.Web.HttpUtility.ParseQueryString(uri.Query)["v"];
        }

        if (videoId is null || !VideoIdRegex().IsMatch(videoId)) return false;

        embedUrl = $"https://www.youtube.com/embed/{videoId}";
        return true;
    }
}
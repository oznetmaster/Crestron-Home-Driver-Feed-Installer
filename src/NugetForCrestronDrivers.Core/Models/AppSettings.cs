namespace NugetForCrestronDrivers.Core.Models;

public sealed class AppSettings
{
    public string FeedUrl { get; set; } = "https://api.nuget.org/v3/index.json";

    public string CacheDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NugetForCrestronDrivers", "Cache");
}

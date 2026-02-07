namespace Api.Models.Options;

public sealed class SerperOptions
{
    public const string SectionName = "Serper";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://google.serper.dev";
    public int TimeoutSeconds { get; set; } = 8;
}

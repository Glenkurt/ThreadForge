namespace Api.Models.Options;

public sealed class XaiOptions
{
    public const string SectionName = "Xai";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.x.ai/v1";
    public string Model { get; set; } = "grok-2-latest";
    public string LightModel { get; set; } = "grok-3-mini-fast";
}

namespace dotnet_server.Application.Services;

public class GaugeOcrOptions
{
    public const string SectionName = "GaugeOcr";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

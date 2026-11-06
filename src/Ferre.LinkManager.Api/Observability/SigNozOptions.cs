namespace Ferre.LinkManager.Api.Observability;

public sealed class SigNozOptions
{
    public const string SectionName = "SigNoz";

    // For example "eu", "us" or "in". It is shown in the SigNoz ingestion settings.
    public string? Region { get; set; }

    public string? IngestionKey { get; set; }

    public string ServiceName { get; set; } = "ferre-link-manager";
}
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Ferre.LinkManager.Api.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(SigNozOptions.SectionName).Get<SigNozOptions>()
            ?? new SigNozOptions();

        // Without a region and an ingestion key nothing is sent anywhere.
        var export = !string.IsNullOrWhiteSpace(options.Region)
                     && !string.IsNullOrWhiteSpace(options.IngestionKey);

        var baseUrl = $"https://ingest.{options.Region?.Trim()}.signoz.cloud:443";

        void ConfigureExporter(OtlpExporterOptions exporter, string path)
        {
            exporter.Endpoint = new Uri(baseUrl + path);
            exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
            exporter.Headers = $"signoz-ingestion-key={options.IngestionKey!.Trim()}";
        }

        // The SQL command logs are noisy. Warnings and errors are still reported.
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(options.ServiceName)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)
                }));

        telemetry.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();

            if (export)
            {
                tracing.AddOtlpExporter(exporter => ConfigureExporter(exporter, "/v1/traces"));
            }
        });

        telemetry.WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation();

            if (export)
            {
                metrics.AddOtlpExporter(exporter => ConfigureExporter(exporter, "/v1/metrics"));
            }
        });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = false;
            logging.IncludeFormattedMessage = false;

            if (export)
            {
                logging.AddOtlpExporter(exporter => ConfigureExporter(exporter, "/v1/logs"));
            }
        });

        return builder;
    }
}
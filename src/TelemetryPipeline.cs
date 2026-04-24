// Copyright (c) Kai Siren. Licensed under the MIT License.

namespace EcoTelemetry;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

/// <summary>
/// Owns the OTel SDK objects (LoggerFactory, MeterProvider, TracerProvider) for the lifetime of the plugin.
/// </summary>
internal sealed class TelemetryPipeline : IDisposable
{
    public ILoggerFactory? LoggerFactory { get; private set; }
    public ILogger? Logger { get; private set; }

    private readonly EcoTelemetryConfig config;

    public TelemetryPipeline(EcoTelemetryConfig config)
    {
        this.config = config;
    }

    public void Start()
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: this.config.ServiceName, serviceVersion: typeof(TelemetryPipeline).Assembly.GetName().Version?.ToString() ?? "0.0.0")
            .AddAttributes(BuildResourceAttributes());

        if (this.config.EnableLogs)
        {
            this.LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                    options.ParseStateValues = true;
                    options.SetResourceBuilder(resourceBuilder);

                    if (string.IsNullOrWhiteSpace(this.config.OtlpEndpoint))
                    {
                        options.AddConsoleExporter();
                    }
                    else
                    {
                        options.AddOtlpExporter(otlp => ConfigureOtlp(otlp));
                    }
                });
            });

            this.Logger = this.LoggerFactory.CreateLogger("EcoTelemetry");
        }
    }

    private void ConfigureOtlp(OtlpExporterOptions otlp)
    {
        otlp.Endpoint = new Uri(this.config.OtlpEndpoint);
        otlp.Protocol = string.Equals(this.config.OtlpProtocol, "Grpc", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.Grpc
            : OtlpExportProtocol.HttpProtobuf;
        if (!string.IsNullOrWhiteSpace(this.config.OtlpHeaders))
        {
            otlp.Headers = this.config.OtlpHeaders;
        }
    }

    private IEnumerable<KeyValuePair<string, object>> BuildResourceAttributes()
    {
        foreach (var kv in this.config.ResourceAttributes)
        {
            yield return new KeyValuePair<string, object>(kv.Key, kv.Value);
        }
    }

    public void Dispose()
    {
        this.LoggerFactory?.Dispose();
        this.LoggerFactory = null;
        this.Logger = null;
    }
}

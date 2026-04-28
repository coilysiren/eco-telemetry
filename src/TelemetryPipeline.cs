// Copyright (c) Kai Siren. Licensed under the MIT License.

namespace EcoTelemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

/// <summary>
/// Owns the OTel SDK objects (LoggerFactory, MeterProvider, TracerProvider) for the lifetime of the plugin.
/// Each signal can route to its own endpoint - logs to Sentry, metrics to VictoriaMetrics, etc.
/// </summary>
internal sealed class TelemetryPipeline : IDisposable
{
    public const string MeterName = "EcoTelemetry";

    public ILoggerFactory? LoggerFactory { get; private set; }
    public ILogger? Logger { get; private set; }
    public MeterProvider? MeterProvider { get; private set; }
    public Meter? Meter { get; private set; }

    private readonly EcoTelemetryConfig config;
    private ResourceBuilder? resourceBuilder;

    public TelemetryPipeline(EcoTelemetryConfig config)
    {
        this.config = config;
    }

    public void Start()
    {
        var version = typeof(TelemetryPipeline).Assembly.GetName().Version?.ToString() ?? "0.0.0";

        this.resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: this.config.ServiceName, serviceVersion: version)
            .AddAttributes(BuildResourceAttributes());

        if (this.config.EnableLogs)
        {
            this.StartLogs();
        }

        if (this.config.EnableMetrics)
        {
            this.StartMetrics(version);
        }
    }

    private void StartLogs()
    {
        this.LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.ParseStateValues = true;
                options.SetResourceBuilder(this.resourceBuilder!);

                if (string.IsNullOrWhiteSpace(this.config.ResolvedLogsEndpoint))
                {
                    options.AddConsoleExporter();
                }
                else
                {
                    options.AddOtlpExporter(otlp => ConfigureOtlp(
                        otlp,
                        this.config.ResolvedLogsEndpoint,
                        this.config.ResolvedLogsProtocol,
                        this.config.ResolvedLogsHeaders));
                }
            });
        });

        this.Logger = this.LoggerFactory.CreateLogger("EcoTelemetry");
    }

    private void StartMetrics(string version)
    {
        this.Meter = new Meter(MeterName, version);

        var builder = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(this.resourceBuilder!)
            .AddMeter(MeterName)
            .AddMeter("OpenTelemetry.Instrumentation.Runtime")
            .AddRuntimeInstrumentation();

        // Diagnostic prints while #5 is open. Goes to stdout -> journal so we
        // can see exactly which branch the config resolution lands in.
        Console.Error.WriteLine($"[EcoTelemetry] StartMetrics: ResolvedMetricsEndpoint=[{this.config.ResolvedMetricsEndpoint}] OtlpMetricsEndpoint=[{this.config.OtlpMetricsEndpoint}] OtlpEndpoint=[{this.config.OtlpEndpoint}] EmitConsoleAlongsideOtlp={this.config.EmitConsoleAlongsideOtlp}");

        if (string.IsNullOrWhiteSpace(this.config.ResolvedMetricsEndpoint))
        {
            Console.Error.WriteLine("[EcoTelemetry] StartMetrics: empty endpoint -> Console-only exporter");
            builder.AddConsoleExporter();
        }
        else
        {
            Console.Error.WriteLine($"[EcoTelemetry] StartMetrics: attaching OTLP exporter to {this.config.ResolvedMetricsEndpoint}");
            // Use the single-arg overload (Action<OtlpExporterOptions>). The
            // two-arg overload (Action<OtlpExporterOptions, MetricReaderOptions>)
            // wasn't producing a visible reader in the pipeline; switching to
            // the simpler shape and configuring reader interval via the
            // OTEL_METRIC_EXPORT_INTERVAL env var instead. Set in
            // eco-server.service systemd Environment= once #5 closes.
            builder.AddOtlpExporter(otlp =>
            {
                ConfigureOtlp(
                    otlp,
                    this.config.ResolvedMetricsEndpoint,
                    this.config.ResolvedMetricsProtocol,
                    this.config.ResolvedMetricsHeaders);
            });
            Console.Error.WriteLine("[EcoTelemetry] StartMetrics: AddOtlpExporter returned");

            // Diagnostic: also emit to console alongside OTLP. Lets us see
            // exactly what the SDK is generating per export tick when an
            // OTLP-side issue isn't immediately obvious. Trim once the
            // pipeline is proven end-to-end (#5).
            if (this.config.EmitConsoleAlongsideOtlp)
            {
                Console.Error.WriteLine("[EcoTelemetry] StartMetrics: also attaching Console exporter (EmitConsoleAlongsideOtlp=true)");
                builder.AddConsoleExporter();
            }
        }

        try
        {
            this.MeterProvider = builder.Build();
            Console.Error.WriteLine("[EcoTelemetry] StartMetrics: MeterProvider built OK");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EcoTelemetry] StartMetrics: MeterProvider build FAILED: {ex}");
            throw;
        }
    }

    private static void ConfigureOtlp(OtlpExporterOptions otlp, string endpoint, string protocol, string headers)
    {
        otlp.Endpoint = new Uri(endpoint);
        otlp.Protocol = string.Equals(protocol, "Grpc", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.Grpc
            : OtlpExportProtocol.HttpProtobuf;
        if (!string.IsNullOrWhiteSpace(headers))
        {
            otlp.Headers = headers;
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
        this.MeterProvider?.Dispose();
        this.MeterProvider = null;
        this.Meter?.Dispose();
        this.Meter = null;
        this.LoggerFactory?.Dispose();
        this.LoggerFactory = null;
        this.Logger = null;
    }
}

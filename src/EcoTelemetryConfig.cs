// Copyright (c) Kai Siren. Licensed under the MIT License.

namespace EcoTelemetry;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Runtime configuration for EcoTelemetry, loaded from Configs/EcoTelemetry.json.</summary>
public sealed class EcoTelemetryConfig
{
    public string ServiceName { get; set; } = "eco-server";

    public Dictionary<string, string> ResourceAttributes { get; set; } = new();

    /// <summary>OTLP endpoint URL used as the fallback for any signal that doesn't have its own override. Empty falls back to console exporter.</summary>
    public string OtlpEndpoint { get; set; } = "";

    /// <summary>"Grpc" or "HttpProtobuf". Defaults to HttpProtobuf since most managed backends accept it.</summary>
    public string OtlpProtocol { get; set; } = "HttpProtobuf";

    /// <summary>Headers in W3C-style "key1=val1,key2=val2" form. Pass auth tokens here.</summary>
    public string OtlpHeaders { get; set; } = "";

    /// <summary>Per-signal endpoint override for logs. Empty falls back to <see cref="OtlpEndpoint"/>. Common case: Sentry for logs, VictoriaMetrics for metrics.</summary>
    public string OtlpLogsEndpoint { get; set; } = "";
    public string OtlpLogsProtocol { get; set; } = "";
    public string OtlpLogsHeaders { get; set; } = "";

    /// <summary>Per-signal endpoint override for metrics.</summary>
    public string OtlpMetricsEndpoint { get; set; } = "";
    public string OtlpMetricsProtocol { get; set; } = "";
    public string OtlpMetricsHeaders { get; set; } = "";

    public string ResolvedLogsEndpoint => Pick(this.OtlpLogsEndpoint, this.OtlpEndpoint);
    public string ResolvedLogsProtocol => Pick(this.OtlpLogsProtocol, this.OtlpProtocol);
    public string ResolvedLogsHeaders => Pick(this.OtlpLogsHeaders, this.OtlpHeaders);
    public string ResolvedMetricsEndpoint => Pick(this.OtlpMetricsEndpoint, this.OtlpEndpoint);
    public string ResolvedMetricsProtocol => Pick(this.OtlpMetricsProtocol, this.OtlpProtocol);
    public string ResolvedMetricsHeaders => Pick(this.OtlpMetricsHeaders, this.OtlpHeaders);

    private static string Pick(string specific, string fallback) => string.IsNullOrWhiteSpace(specific) ? fallback : specific;

    public bool EnableLogs { get; set; } = true;
    public bool EnableMetrics { get; set; } = false;
    public bool EnableTraces { get; set; } = false;

    public int MetricsIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Subscribe to AppDomain.FirstChanceException. Catches every thrown exception, including caught ones.
    /// High-volume on a busy server. Off by default; flip on for short diagnostic windows.
    /// </summary>
    public bool FirstChanceExceptionsEnabled { get; set; } = false;

    /// <summary>
    /// Wrap Eco's ILogWriter so warnings and errors flow through the OTel logs pipeline. Best-effort, uses reflection.
    /// </summary>
    public bool InterceptLogWriter { get; set; } = true;

    public static EcoTelemetryConfig Load(string path)
    {
        if (!File.Exists(path)) return new EcoTelemetryConfig();
        var json = File.ReadAllText(path);
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        return JsonSerializer.Deserialize<EcoTelemetryConfig>(json, opts) ?? new EcoTelemetryConfig();
    }
}

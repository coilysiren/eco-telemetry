// Copyright (c) Kai Siren. Licensed under the MIT License.

namespace EcoTelemetry;

using System;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Eco.Gameplay.Players;
using Microsoft.Extensions.Logging;

/// <summary>
/// Registers Eco-specific observable instruments on the supplied <see cref="Meter"/>.
/// Gauges are pull-based: the OTel reader polls the callbacks on its export interval, so there's no per-tick work
/// to do beyond keeping the worker thread alive until shutdown.
/// </summary>
internal sealed class MetricsWorker
{
    private readonly EcoTelemetryConfig config;
    private readonly ILogger? logger;

    public MetricsWorker(EcoTelemetryConfig config, ILogger? logger)
    {
        this.config = config;
        this.logger = logger;
    }

    public void Install(Meter meter)
    {
        if (!this.config.EnableMetrics) return;

        meter.CreateObservableGauge(
            name: "eco.players.online",
            observeValue: SafeOnlineUserCount,
            unit: "{players}",
            description: "Currently logged-in players.");

        this.logger?.LogInformation("EcoTelemetry: registered metrics (eco.players.online + runtime).");
    }

    private static int SafeOnlineUserCount()
    {
        try
        {
            return UserManager.Obj?.OnlineUserCount ?? 0;
        }
        catch
        {
            // Eco's UserManager may not be ready during early init; report 0 rather than throwing into the OTel reader.
            return 0;
        }
    }

    public Task TickAsync(CancellationToken token)
    {
        // Worker is no longer the metrics driver - OTel's PeriodicExportingMetricReader polls observable instruments
        // on its own interval. Just block until shutdown so IWorkerPlugin's loop stays parked.
        return Task.Delay(Timeout.Infinite, token);
    }
}

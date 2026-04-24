// Copyright (c) Kai Siren. Licensed under the MIT License.

namespace EcoTelemetry;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Stub for the metrics surface. Helper invoked from <see cref="EcoTelemetryPlugin.DoWork"/>. v1 placeholder.
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

    public async Task TickAsync(CancellationToken token)
    {
        if (!this.config.EnableMetrics)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
            return;
        }

        try
        {
            // TODO(metrics): poll and emit
            //   - UserManager.OnlineUsers.Count → eco.players.online (gauge)
            //   - WorldObjectManager.Obj counts → eco.world_objects.{type} (gauge)
            //   - Simulation.Time.WorldTime.Seconds → eco.world.time_seconds (counter)
            //   - GC and threadpool come for free via OpenTelemetry.Instrumentation.Runtime
            //   - PluginManager.GetPlugin<Stats>() → derived economy counters
            this.logger?.LogTrace("MetricsWorker tick (stub)");
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "MetricsWorker tick failed");
        }

        await Task.Delay(TimeSpan.FromSeconds(this.config.MetricsIntervalSeconds), token).ConfigureAwait(false);
    }
}

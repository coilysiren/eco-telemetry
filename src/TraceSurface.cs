// Copyright (c) Kai Siren. Licensed under the MIT License.

namespace EcoTelemetry;

using System.Diagnostics;

/// <summary>
/// Stub for the traces surface. v1 placeholder. The plan is to expose a single ActivitySource that the rest of the
/// mod can wrap around plugin init, slow handler detection, and web-request hooks once those integration points
/// are scoped.
/// </summary>
internal static class TraceSurface
{
    public const string ActivitySourceName = "EcoTelemetry";

    public static readonly ActivitySource Source = new(ActivitySourceName);

    // TODO(traces):
    //   - wrap PluginManager init paths in spans (will need reflection or a public hook we don't have yet)
    //   - emit a span on any handler that exceeds N ms (slow-handler detector, polled from a worker)
    //   - decorate the embedded Kestrel server's request pipeline if a hook can be found in EcoModKit
}

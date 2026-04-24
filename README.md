# EcoTelemetry

OpenTelemetry-backed observability for [Eco](https://play.eco) game servers. Exports exceptions, runtime metrics, simulation perf, and traces over OTLP so server admins can point them at Sentry, Grafana, Datadog, Honeycomb, or any other OTLP-compatible backend.

Eco's built-in stats system is designed for in-game economic and ecological reporting. It does not surface SRE-shaped signals: process health, exception rates, simulation tick latency, or web-request latency. EcoTelemetry fills that gap.

## Status

Early. v1 ships exception capture end-to-end via OTLP. Metrics and traces are stubbed and tracked as follow-ups.

## Install

1. Download `EcoTelemetry-<version>.zip` from the [latest release](https://github.com/coilysiren/eco-telemetry/releases).
2. Extract into your Eco server's `Mods/` directory so the result looks like `Mods/EcoTelemetry/EcoTelemetry.dll`.
3. Copy `Configs/EcoTelemetry.example.json` to `Configs/EcoTelemetry.json` and edit:
   - `OtlpEndpoint`: your collector's OTLP HTTP or gRPC endpoint
   - `OtlpHeaders`: any auth headers (e.g. `x-honeycomb-team=...` or Sentry's auth header)
   - `ServiceName`, `ResourceAttributes`: how this server identifies itself
   - per-signal toggles: `EnableLogs`, `EnableMetrics`, `EnableTraces`
4. Restart the Eco server.

If `OtlpEndpoint` is empty, EcoTelemetry falls back to the console exporter so you can verify the pipeline locally before pointing it at a real backend.

## Build

```bash
dotnet build EcoTelemetry.csproj -c Release
```

The build pulls `Eco.ReferenceAssemblies` from NuGet for type-checking. The DLL plus its OpenTelemetry dependencies must be deployed together — see `.github/workflows/release.yml` for how the release artifact is assembled.

## Sending to Sentry

Sentry's OTLP endpoint is documented at <https://docs.sentry.io/platforms/javascript/install/cdn/#using-opentelemetry>. Set:

- `OtlpEndpoint`: `https://o<org>.ingest.sentry.io/api/<project>/otlp/v1/logs` (and matching `/v1/traces` when traces ship)
- `OtlpHeaders`: `x-sentry-auth=Sentry sentry_key=<dsn-public-key>`

Sentry is one OTLP destination among many. There is no Sentry-specific code path.

## Public sources

EcoTelemetry is built against [Eco.ReferenceAssemblies](https://www.nuget.org/packages/Eco.ReferenceAssemblies) and the public modding documentation at <https://wiki.play.eco/en/Modding>, <https://docs.play.eco/>, and <https://github.com/StrangeLoopGames/EcoModKit>.

## License

MIT. See [LICENSE](LICENSE).

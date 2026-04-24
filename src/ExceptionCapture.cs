// Copyright (c) Kai Siren. Licensed under the MIT License.

namespace EcoTelemetry;

using System;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;

/// <summary>
/// Subscribes to AppDomain exception hooks and forwards them through the OTel logs pipeline.
/// AppDomain.UnhandledException catches fatal exits. FirstChanceException catches every throw, including caught ones,
/// which is high-volume on a busy server and gated behind config.
/// </summary>
internal sealed class ExceptionCapture : IDisposable
{
    private readonly ILogger logger;
    private readonly bool firstChanceEnabled;
    private bool installed;

    public ExceptionCapture(ILogger logger, bool firstChanceEnabled)
    {
        this.logger = logger;
        this.firstChanceEnabled = firstChanceEnabled;
    }

    public void Install()
    {
        if (this.installed) return;
        AppDomain.CurrentDomain.UnhandledException += this.OnUnhandledException;
        if (this.firstChanceEnabled)
        {
            AppDomain.CurrentDomain.FirstChanceException += this.OnFirstChanceException;
        }
        this.installed = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            this.logger.LogCritical(ex, "Unhandled exception (terminating={IsTerminating})", e.IsTerminating);
        }
        else
        {
            this.logger.LogCritical("Unhandled non-Exception object: {Obj}", e.ExceptionObject);
        }
    }

    private void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
        // FirstChanceException fires for every thrown exception before any catch handler runs. Be defensive: a logging
        // failure here can re-enter and recurse.
        try
        {
            this.logger.LogWarning(e.Exception, "First-chance exception: {Type}", e.Exception.GetType().FullName);
        }
        catch
        {
            // swallow; we cannot afford to throw inside the first-chance handler
        }
    }

    public void Dispose()
    {
        if (!this.installed) return;
        AppDomain.CurrentDomain.UnhandledException -= this.OnUnhandledException;
        if (this.firstChanceEnabled)
        {
            AppDomain.CurrentDomain.FirstChanceException -= this.OnFirstChanceException;
        }
        this.installed = false;
    }
}

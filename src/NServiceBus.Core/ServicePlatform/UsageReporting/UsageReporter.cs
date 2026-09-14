#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus.Features;

partial class UsageReporter(
    IServicePlatformSender<EndpointUsageReport> usageReportSender,
    UsageReporterSettings settings,
    TimeProvider timeProvider,
    ILogger<UsageReporter> logger
) : FeatureStartupTask, IDisposable
{
    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
    {
        shutdownTokenSource = new CancellationTokenSource();
        ConfigureMeterListener();

        reporterTask = PeriodicallyReportUsageAndSwallowExceptions(shutdownTokenSource.Token);

        return Task.CompletedTask;
    }

    protected override async Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
    {
        shutdownTokenSource?.Cancel();

        if (reporterTask is not null)
        {
            await reporterTask.ConfigureAwait(false);
        }

        await SnapshotAndSendUsageReport(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        meterListener?.Dispose();
        shutdownTokenSource?.Dispose();
    }

    void ConfigureMeterListener()
    {
        meterListener = new();
        meterListener.InstrumentPublished += (instrument, listener) =>
        {
            if (IsSuccessfulMessageProcessingEvent(instrument))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (IsSuccessfulMessageProcessingEvent(instrument))
            {
                for (var i = 0; i < tags.Length; i++)
                {
                    var tag = tags[i];
                    if (tag.Key == MeterTags.QueueName)
                    {
                        if (tag.Value?.ToString() == settings.BaseQueueAddress)
                        {
                            // Update the internal counter
                            _ = Interlocked.Add(ref messagesSuccessfullyProcessed, measurement);
                            return;
                        }
                    }
                }
            }
        });

        meterListener.Start();

        static bool IsSuccessfulMessageProcessingEvent(Instrument instrument)
            => instrument.Meter.Name == IncomingPipelineMetrics.MeterName && instrument.Name == IncomingPipelineMetrics.TotalProcessedSuccessfully;
    }

    async Task PeriodicallyReportUsageAndSwallowExceptions(CancellationToken cancellationToken)
    {
        LogStartup(logger);

        using var periodicTimer = new PeriodicTimer(settings.ReportingInterval, timeProvider);

        try
        {
            while (await periodicTimer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    await SnapshotAndSendUsageReport(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    LogErrorWhileReportingUsage(logger, ex);
                }
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            LogOperationCancelled(logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting usage reporting task.")]
    static partial void LogStartup(ILogger logger);
    [LoggerMessage(Level = LogLevel.Warning, Message = "An error occurred while reporting usage information.")]
    static partial void LogErrorWhileReportingUsage(ILogger logger, Exception ex);
    [LoggerMessage(Level = LogLevel.Debug, Message = "Operation cancelled while reporting usage information. This is expected when the endpoint is shutting down")]
    static partial void LogOperationCancelled(ILogger logger, Exception ex);

    async Task SnapshotAndSendUsageReport(CancellationToken cancellationToken)
    {
        var currentSnapshot = Interlocked.Read(ref messagesSuccessfullyProcessed);

        var message = new EndpointUsageReport
        {
            EndpointName = settings.EndpointName,
            TimeStamp = timeProvider.GetUtcNow(),
            MessagesSuccessfullyProcessed = currentSnapshot - previousSnapshot
        };

        await usageReportSender.Send(message, cancellationToken).ConfigureAwait(false);

        previousSnapshot = currentSnapshot;
    }

    long previousSnapshot = 0;
    long messagesSuccessfullyProcessed = 0;
    MeterListener? meterListener;
    CancellationTokenSource? shutdownTokenSource;
    Task? reporterTask;
}

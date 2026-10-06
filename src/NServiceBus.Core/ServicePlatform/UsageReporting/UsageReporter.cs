#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus.Features;
using NServiceBus.ServicePlatform;

partial class UsageReporter(
    ServicePlatformChannel servicePlatformChannel,
    UsageReporterSettings settings,
    TimeProvider timeProvider,
    ILogger<UsageReporter> logger
) : FeatureStartupTask, IDisposable
{
    readonly ServicePlatformSender<EndpointUsageReport> usageReportSender = servicePlatformChannel.CreateSender(UsageReportingMessagesJsonContext.Default.EndpointUsageReport);
    readonly KeyValuePair<string, object?> queueNameTag = new(MeterTags.QueueName, settings.BaseQueueAddress);

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

        try
        {
            await SnapshotAndSendUsageReportAndSwallowExceptions(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            LogOperationCancelled(logger, ex);
        }

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
                listener.EnableMeasurementEvents(instrument, this);
            }
        };

        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (IsSuccessfulMessageProcessingEvent(instrument) && tags.IndexOf(queueNameTag) >= 0)
            {
                var usageReporterState = (UsageReporter)state!;
                // Update the internal counter
                _ = Interlocked.Add(ref usageReporterState.messagesSuccessfullyProcessed, measurement);
            }
        });

        meterListener.Start();

        static bool IsSuccessfulMessageProcessingEvent(Instrument instrument)
            => instrument is { Meter.Name: IncomingPipelineMetrics.MeterName, Name: IncomingPipelineMetrics.TotalProcessedSuccessfully };
    }

    async Task PeriodicallyReportUsageAndSwallowExceptions(CancellationToken cancellationToken)
    {
        LogStartup(logger);

        using var periodicTimer = new PeriodicTimer(settings.ReportingInterval, timeProvider);

        try
        {
            while (await periodicTimer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await SnapshotAndSendUsageReportAndSwallowExceptions(cancellationToken).ConfigureAwait(false);
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

    async Task SnapshotAndSendUsageReportAndSwallowExceptions(CancellationToken cancellationToken)
    {
        try
        { 
            var currentSnapshot = Interlocked.Read(ref messagesSuccessfullyProcessed);

            // TODO: Send scope if we're able to determine it (i.e. vhost for RabbitMQ, Catalog/Schema for SQL)
            var message = new EndpointUsageReport
            {
                EndpointName = settings.EndpointName,
                TimeStamp = timeProvider.GetUtcNow(),
                MessagesSuccessfullyProcessed = currentSnapshot - previousSnapshot
            };

            await usageReportSender.Send(message, cancellationToken).ConfigureAwait(false);

            previousSnapshot = currentSnapshot;
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

    long previousSnapshot = 0;
    long messagesSuccessfullyProcessed = 0;
    MeterListener? meterListener;
    CancellationTokenSource? shutdownTokenSource;
    Task? reporterTask;
}

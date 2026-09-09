#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Features;
using NServiceBus.Logging;

class UsageReporter(ServicePlatformSender<EndpointUsageReport> usageReportSender, UsageReporterSettings settings) : FeatureStartupTask, IDisposable
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
                // Update the internal counter
                // TODO: See if we're tagged for the right endpoint, and maybe separate counts by queue
                _ = Interlocked.Add(ref messagesSuccessfullyProcessed, measurement);
            }
        });

        // TODO: Move magic strings into constants shared by the two components
        static bool IsSuccessfulMessageProcessingEvent(Instrument instrument)
            => instrument.Meter.Name == "NServiceBus.Core.Pipeline.Incoming" && instrument.Name == "nservicebus.messaging.successes";
    }

    async Task PeriodicallyReportUsageAndSwallowExceptions(CancellationToken cancellationToken)
    {
        Logger.Debug("Starting usage reporting task.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(settings.ReportingInterval, cancellationToken).ConfigureAwait(false);
                await SnapshotAndSendUsageReport(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                Logger.Debug("Operation cancelled while reporting usage information. This is expected when the endpoint is shutting down.", ex);
                break;
            }
            catch (Exception ex)
            {
                Logger.Warn("An error occurred while reporting usage information.", ex);
            }
        }
    }

    async Task SnapshotAndSendUsageReport(CancellationToken cancellationToken)
    {
        var currentSnapshot = Interlocked.Read(ref messagesSuccessfullyProcessed);

        var message = new EndpointUsageReport
        {
            EndpointName = settings.EndpointName,
            TimeStamp = DateTimeOffset.UtcNow,
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

    static readonly ILog Logger = LogManager.GetLogger<UsageReporter>();
}

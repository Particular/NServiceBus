namespace NServiceBus;

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Features;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Settings;
using Transport;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

partial class StartableEndpoint(
    SettingsHolder settings,
    FeatureComponent featureComponent,
    EnvelopeComponent envelopeComponent,
    ReceiveComponent receiveComponent,
    TransportSeam transportSeam,
    PipelineComponent pipelineComponent,
    RecoverabilityComponent recoverabilityComponent,
    HostingComponent hostingComponent,
    SendComponent sendComponent,
    IServiceProvider serviceProvider,
    MessageSession messageSession,
    IAsyncDisposable serviceProviderLease)
{
    public async Task RunInstallers(CancellationToken cancellationToken = default)
    {
        using var _ = LogManager.BeginSlotScope(hostingComponent.Config.EndpointLogSlot);
        await hostingComponent.RunInstallers(serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    public async Task Setup(CancellationToken cancellationToken = default)
    {
        using var _ = LogManager.BeginSlotScope(hostingComponent.Config.EndpointLogSlot);

        // Resolved here because the shutdown paths can run while the service provider is being disposed.
        logger = serviceProvider.GetRequiredService<ILogger<StartableEndpoint>>();

        transportInfrastructure = await transportSeam.CreateTransportInfrastructure(serviceProvider, hostingComponent.ShouldRunInstallers(serviceProvider), cancellationToken).ConfigureAwait(false);

        // The message session keeps the stopping token for later operations; setup itself only uses cancellationToken.
#pragma warning disable PS0021
        try
#pragma warning restore PS0021
        {
            var pipelineCache = pipelineComponent.BuildPipelineCache(serviceProvider);
            var messageOperations = sendComponent.CreateMessageOperations(serviceProvider, pipelineComponent);
            stoppingTokenSource = new CancellationTokenSource();

            messageSession.Initialize(serviceProvider, messageOperations, pipelineCache, stoppingTokenSource.Token);

            var consecutiveFailuresConfig = settings.Get<ConsecutiveFailuresConfiguration>();

            await receiveComponent.Initialize(serviceProvider, recoverabilityComponent, envelopeComponent, messageOperations, pipelineComponent, pipelineCache, transportInfrastructure, consecutiveFailuresConfig, cancellationToken).ConfigureAwait(false);

            AddSendingQueueManifest();
        }
#pragma warning disable PS0019 // Cleanup runs for cancellation too; the exception is rethrown
        catch (Exception)
#pragma warning restore PS0019
        {
            transportShutDown = true;
            await ShutdownTransport(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    // Once Start has created the running instance, that instance owns shutdown, so this method does nothing.
    public async Task ShutdownWithoutRunning(CancellationToken cancellationToken = default)
    {
        await startSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (runningInstanceCreated || transportShutDown || transportInfrastructure is null)
            {
                return;
            }

            transportShutDown = true;

            using var _ = LogManager.BeginSlotScope(hostingComponent.Config.EndpointLogSlot);
            await ShutdownTransport(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            startSemaphore.Release();
        }
    }

    async Task ShutdownTransport(CancellationToken cancellationToken)
    {
        // Both steps run even when the first is canceled, so the transport always gets Shutdown.
        // A canceled step is not reported, because the caller canceled and any original exception stays more relevant.
        try
        {
            await receiveComponent.Stop(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            LogStoppingReceiversFailed(logger, ex);
        }
        catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
        {
        }

        try
        {
            await transportInfrastructure.Shutdown(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            LogTransportShutdownFailed(logger, ex);
        }
        catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
        {
        }
    }

    void AddSendingQueueManifest()
    {
        hostingComponent.Config.AddStartupDiagnosticsSection("Manifest-SendingQueues", transportSeam.QueueBindings.SendingAddresses.ToList(), StartupDiagnosticsJsonContext.Default.ListString);
        hostingComponent.Config.AddStartupDiagnosticsSection("Manifest-ErrorQueue", settings.ErrorQueueAddress(), StartupDiagnosticsJsonContext.Default.String);
    }

    public async Task<RunningEndpointInstance> Start(CancellationToken cancellationToken = default)
    {
        if (endpointInstance is not null)
        {
            return endpointInstance;
        }

        await startSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (endpointInstance is not null)
            {
                return endpointInstance;
            }

            if (runningInstanceCreated || transportShutDown)
            {
                throw new InvalidOperationException("The endpoint failed to start or was shut down without running, and cannot be started.");
            }

            using var _ = LogManager.BeginSlotScope(hostingComponent.Config.EndpointLogSlot);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            }

            // Created before anything starts so that a failed start, and a critical error raised while starting,
            // both stop the endpoint through the same StopCore sequence.
            var runningInstance = new RunningEndpointInstance(settings, receiveComponent, featureComponent, messageSession, transportInfrastructure, stoppingTokenSource, serviceProviderLease, hostingComponent.Config.EndpointLogSlot);
            runningInstanceCreated = true;

            hostingComponent.SetupCriticalErrors(runningInstance, cancellationToken);

            try
            {
                await featureComponent.StartFeatures(serviceProvider, messageSession, cancellationToken).ConfigureAwait(false);

                await hostingComponent.WriteDiagnosticsFile(cancellationToken).ConfigureAwait(false);

                await receiveComponent.Start(cancellationToken).ConfigureAwait(false);
            }
#pragma warning disable PS0019 // Cleanup runs for cancellation too; the exception is rethrown
            catch (Exception)
#pragma warning restore PS0019
            {
                await StopAfterFailedStart(runningInstance, logger, cancellationToken).ConfigureAwait(false);
                throw;
            }

            endpointInstance = runningInstance;
            return endpointInstance;
        }
        finally
        {
            startSemaphore.Release();
        }
    }

    static async Task StopAfterFailedStart(RunningEndpointInstance runningInstance, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            await runningInstance.Stop(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            LogStopAfterFailedStartFailed(logger, ex);
        }
#pragma warning disable PS0019
        catch (Exception)
#pragma warning restore PS0019
        {
            // The caller canceled the start, so an aborted cleanup is expected. The start exception stays more relevant to the caller.
        }
    }

    TransportInfrastructure transportInfrastructure;
    CancellationTokenSource stoppingTokenSource;
    volatile RunningEndpointInstance endpointInstance = null!;
    bool runningInstanceCreated;
    bool transportShutDown;
    readonly SemaphoreSlim startSemaphore = new(1, 1);
    ILogger logger = null!;

    [LoggerMessage(LogLevel.Error, "Stopping the receivers failed.")]
    static partial void LogStoppingReceiversFailed(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Shutdown of the transport infrastructure failed.")]
    static partial void LogTransportShutdownFailed(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Stopping the endpoint after it failed to start failed.")]
    static partial void LogStopAfterFailedStartFailed(ILogger logger, Exception exception);
}
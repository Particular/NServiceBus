#nullable enable

namespace NServiceBus.Features;

using System;
using System.Threading;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.DependencyInjection;

interface IFeatureStartupTaskController
{
    string Name { get; }
    Task Start(IServiceProvider builder, IMessageSession messageSession, CancellationToken cancellationToken = default);
    Task Stop(IMessageSession messageSession, CancellationToken cancellationToken = default);
}

class ActivatorBasedFeatureStartupTaskController<TTask> : IFeatureStartupTaskController
    where TTask : FeatureStartupTask
{
    public string Name { get; } = typeof(TTask).Name;

    public Task Start(IServiceProvider builder, IMessageSession messageSession, CancellationToken cancellationToken = default)
    {
        if (Log.IsDebugEnabled)
        {
            Log.Debug($"Starting {nameof(FeatureStartupTask)} '{Name}'.");
        }

        instance = ActivatorUtilities.CreateInstance<TTask>(provider: builder);
        return instance.PerformStartup(messageSession, cancellationToken);
    }

    public async Task Stop(IMessageSession messageSession, CancellationToken cancellationToken = default)
    {
        if (instance == null)
        {
            return;
        }

        try
        {
            await instance.PerformStop(messageSession, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            Log.Warn($"Exception occurred during stopping of feature startup task '{Name}'.", ex);
        }
        finally
        {
            if (instance is IAsyncDisposable asyncDisposableInstaller)
            {
                await asyncDisposableInstaller.DisposeAsync().ConfigureAwait(false);
            }
            else if (instance is IDisposable disposableInstaller)
            {
                disposableInstaller.Dispose();
            }
        }
    }

    TTask? instance;

    static readonly ILog Log = LogManager.GetLogger<Task>();
}

class FeatureStartupTaskController : IFeatureStartupTaskController
{
    public FeatureStartupTaskController(string name, Func<IServiceProvider, FeatureStartupTask> factory)
    {
        Name = name;
        this.factory = factory;
    }

    public string Name { get; }

    public Task Start(IServiceProvider builder, IMessageSession messageSession, CancellationToken cancellationToken = default)
    {
        if (Log.IsDebugEnabled)
        {
            Log.Debug($"Starting {nameof(FeatureStartupTask)} '{Name}'.");
        }

        instance = factory(builder);
        return instance.PerformStartup(messageSession, cancellationToken);
    }

    public async Task Stop(IMessageSession messageSession, CancellationToken cancellationToken = default)
    {
        if (instance == null)
        {
            return;
        }

        try
        {
            await instance.PerformStop(messageSession, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            Log.Warn($"Exception occurred during stopping of feature startup task '{Name}'.", ex);
        }
        finally
        {
            DisposeIfNecessary(instance);
        }
    }

    static void DisposeIfNecessary(FeatureStartupTask task)
    {
        var disposableTask = task as IDisposable;
        disposableTask?.Dispose();
    }

    readonly Func<IServiceProvider, FeatureStartupTask> factory;
    FeatureStartupTask? instance;

    static readonly ILog Log = LogManager.GetLogger<FeatureStartupTaskController>();
}
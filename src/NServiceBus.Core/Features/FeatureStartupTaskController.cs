#nullable enable

namespace NServiceBus.Features;

using System;
using System.Threading;
using System.Threading.Tasks;
using Logging;

class FeatureStartupTaskController(string name, Func<IServiceProvider, FeatureStartupTask> factory)
    : FeatureStartupTaskController<Func<IServiceProvider, FeatureStartupTask>>(name,
        static (provider, state) => state(provider), factory);

class FeatureStartupTaskController<TState>(string name, Func<IServiceProvider, TState, FeatureStartupTask> factory, TState state)
    : IFeatureStartupTaskController
{
    public string Name { get; } = name;

    public async Task Start(IServiceProvider builder, IMessageSession messageSession, CancellationToken cancellationToken = default)
    {
        if (Log.IsDebugEnabled)
        {
            Log.Debug($"Starting {nameof(FeatureStartupTask)} '{Name}'.");
        }

        instance = factory(builder, state);
        await instance.PerformStartup(messageSession, cancellationToken).ConfigureAwait(false);
        started = true;
    }

    public async Task Stop(IMessageSession messageSession, CancellationToken cancellationToken = default)
    {
        // An endpoint that fails to start stops its features after StartFeatures has already rolled back,
        // so a task that never started or was already stopped must not be stopped again.
        if (instance == null || !started)
        {
            return;
        }

        started = false;

        try
        {
            await using var _ = Disposable.Wrap(instance).ConfigureAwait(false);
            await instance.PerformStop(messageSession, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            Log.Warn($"Exception occurred during stopping of feature startup task '{Name}'.", ex);
        }
    }

    FeatureStartupTask? instance;
    bool started;

    static readonly ILog Log = LogManager.GetLogger("FeatureStartupTaskController");
}
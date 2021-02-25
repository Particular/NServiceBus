namespace NServiceBus.Features
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    class FeatureStartupTaskController
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

        public async Task Stop(CancellationToken cancellationToken = default)
        {
            try
            {
                await instance.PerformStop(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Log.Warn($"Exception occurred during stopping of feature startup task '{Name}'.", exception);
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

        Func<IServiceProvider, FeatureStartupTask> factory;
        FeatureStartupTask instance;

        static ILog Log = LogManager.GetLogger<FeatureStartupTaskController>();
    }
}
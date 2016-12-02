namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using ObjectBuilder;

    class FeatureStartupTaskController
    {
        public FeatureStartupTaskController(string name, Func<IBuilder, FeatureStartupTask> factory)
        {
            Name = name;
            this.factory = factory;
        }

        public string Name { get; }

        public Task Start(IBuilder builder, IMessageSession messageSession)
        {
            instance = factory(builder);
            return instance.PerformStartup(messageSession);
        }

        public async Task Stop(IMessageSession messageSession)
        {
            try
            {
                await instance.PerformStop(messageSession).ConfigureAwait(false);
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

        Func<IBuilder, FeatureStartupTask> factory;
        FeatureStartupTask instance;

        static ILog Log = LogManager.GetLogger<FeatureStartupTaskController>();
    }
}
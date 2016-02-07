namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.ObjectBuilder;

    class FeatureStartupTaskController
    {
        public FeatureStartupTaskController(string name, Func<IBuilder, FeatureStartupTask> factory)
        {
            Name = name;
            this.factory = factory;
        }

        public string Name { get; }
        Func<IBuilder, FeatureStartupTask> factory;
        FeatureStartupTask instance;

        public Task Start(IBuilder builder, IMessageSession messageSession)
        {
            instance = factory(builder);
            return instance.PerformStartup(messageSession);
        }

        public async Task Stop(IMessageSession messageSession)
        {
            await instance.PerformStop(messageSession).ConfigureAwait(false);
            DisposeIfNecessary(instance);
        }

        static void DisposeIfNecessary(FeatureStartupTask task)
        {
            var disposableTask = task as IDisposable;
            disposableTask?.Dispose();
        }
    }
}
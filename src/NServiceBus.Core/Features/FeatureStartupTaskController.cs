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

        public Task Start(IBuilder builder, IBusSession busSession)
        {
            instance = factory(builder);
            return instance.PerformStartup(busSession);
        }

        public async Task Stop(IBusSession IBusSession)
        {
            await instance.PerformStop(IBusSession).ConfigureAwait(false);
            DisposeIfNecessary(instance);
        }

        static void DisposeIfNecessary(FeatureStartupTask task)
        {
            var disposableTask = task as IDisposable;
            disposableTask?.Dispose();
        }
    }
}
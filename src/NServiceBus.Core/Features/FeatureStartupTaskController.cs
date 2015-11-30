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

        public Task Start(IBuilder builder, IBusContext busContext)
        {
            instance = factory(builder);
            return instance.PerformStartup(busContext);
        }

        public async Task Stop(IBusContext busContext)
        {
            await instance.PerformStop(busContext);
            DisposeIfNecessary(instance);
        }

        static void DisposeIfNecessary(FeatureStartupTask task)
        {
            var disposableTask = task as IDisposable;
            disposableTask?.Dispose();
        }
    }
}
namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using NServiceBus.DataBus;

    /// <summary>
    /// Used to configure the databus.
    /// </summary>
    public class DataBus : Feature, IRequireService<DataBusStorage>
    {
        /// <summary>
        /// Called when the features is activated.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            //todo: this can be a service dependency instead
            if (!context.Container.HasComponent<IDataBusSerializer>())
            {
                context.Container.ConfigureComponent<DefaultDataBusSerializer>(DependencyLifecycle.SingleInstance);
            }

            var storage = context.GetService<DataBusStorage>().Storage;

            context.RegisterStartupTask(b => new IDataBusInitializer(storage));

            var conventions = context.Settings.Get<Conventions>();
            context.Pipeline.Register(new DataBusReceiveBehavior.Registration(conventions));
            context.Pipeline.Register(new DataBusSendBehavior.Registration(conventions));
        }

        class IDataBusInitializer : FeatureStartupTask
        {
            readonly IDataBus storage;

            public IDataBusInitializer(IDataBus storage)
            {
                this.storage = storage;
            }

            protected override Task OnStart(IMessageSession session)
            {
                return storage.Start();
            }

            protected override Task OnStop(IMessageSession session)
            {
                return TaskEx.CompletedTask;
            }
        }
    }
}
namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.DataBus;
    using Settings;

    /// <summary>
    /// Used to configure the databus.
    /// </summary>
    public class DataBus : Feature
    {
        internal DataBus()
        {
            Defaults(s => s.EnableFeatureByDefault(GetSelectedFeatureForDataBus(s)));
        }

        static Type GetSelectedFeatureForDataBus(SettingsHolder settings)
        {
            if (!settings.TryGet("SelectedDataBus", out DataBusDefinition dataBusDefinition))
            {
                dataBusDefinition = new FileShareDataBus();
            }

            return dataBusDefinition.ProvidedByFeature();
        }

        /// <summary>
        /// Called when the features is activated.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Container.HasComponent<IDataBusSerializer>())
            {
                context.Container.ConfigureComponent<DefaultDataBusSerializer>(DependencyLifecycle.SingleInstance);
            }

            context.RegisterStartupTask(b => new IDataBusInitializer(b.GetService<IDataBus>()));

            var conventions = context.Settings.Get<Conventions>();
            context.Pipeline.Register(new DataBusReceiveBehavior.Registration(conventions));
            context.Pipeline.Register(new DataBusSendBehavior.Registration(conventions));
        }

        class IDataBusInitializer : FeatureStartupTask
        {
            IDataBus dataBus;

            public IDataBusInitializer(IDataBus dataBus)
            {
                this.dataBus = dataBus;
            }

            protected override Task OnStart(IMessageSession session)
            {
                return dataBus.Start();
            }

            protected override Task OnStop(IMessageSession session)
            {
                return TaskEx.CompletedTask;
            }
        }
    }
}
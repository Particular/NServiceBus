namespace NServiceBus.Features
{
    using System;
    using NServiceBus.DataBus;
    using NServiceBus.Settings;

    /// <summary>
    /// Used to configure the databus. 
    /// </summary>
    public class DataBus : Feature
    {
        internal DataBus()
        {
            Defaults(s => s.EnableFeatureByDefault(GetSelectedFeatureForDataBus(s)));

            RegisterStartupTask<IDataBusInitializer>();
        }

        static Type GetSelectedFeatureForDataBus(SettingsHolder settings)
        {
            DataBusDefinition dataBusDefinition;

            if (!settings.TryGet("SelectedDataBus", out dataBusDefinition))
            {
                dataBusDefinition = new FileShareDataBus();
            }

            return dataBusDefinition.ProvidedByFeature();
        }

        class IDataBusInitializer : FeatureStartupTask
        {
            public IDataBus DataBus { get; set; }

            protected override void OnStart()
            {
                DataBus.Start();
            }
        }

        /// <summary>
        ///     Called when the features is activated
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Container.HasComponent<IDataBusSerializer>())
            {
                context.Container.ConfigureComponent<DefaultDataBusSerializer>(DependencyLifecycle.SingleInstance);
            }

            context.Pipeline.Register<DataBusReceiveBehavior.Registration>();
            context.Pipeline.Register<DataBusSendBehavior.Registration>();
        }
    }
}
namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
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

        // This feature envies the FileShareDataBus feature. In this case probably the DataBus feature should be abstract the the FileShareDatabus feature should implement it
        static Type GetSelectedFeatureForDataBus(ReadOnlySettings settings)
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

            protected override Task OnStart(IBusContext context)
            {
                return DataBus.Start();
            }
        }

        /// <summary>
        ///     Called when the features is activated.
        /// </summary>
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            if (!context.Container.HasComponent<IDataBusSerializer>())
            {
                context.Container.ConfigureComponent<DefaultDataBusSerializer>(DependencyLifecycle.SingleInstance);
            }

            context.Pipeline.Register<DataBusReceiveBehavior.Registration>();
            context.Pipeline.Register<DataBusSendBehavior.Registration>();

            return FeatureStartupTask.None;
        }
    }
}
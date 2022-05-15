namespace NServiceBus.Features
{
    using System;
    using System.Threading;
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
            if (!settings.TryGet(SelectedDataBusKey, out DataBusDefinition dataBusDefinition))
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
            var serializerType = context.Settings.Get<Type>(DataBusSerializerTypeKey);
            var conventions = context.Settings.Get<Conventions>();

            if (!context.Container.HasComponent<IDataBusSerializer>())
            {
                context.Container.ConfigureComponent(serializerType, DependencyLifecycle.SingleInstance);
            }

            context.RegisterStartupTask(b => new DataBusInitializer(b.GetRequiredService<IDataBus>()));

            context.Pipeline.Register(new DataBusReceiveBehavior.Registration(b =>
            {
                var mainSerializer = b.GetRequiredService<IDataBusSerializer>();

                IDataBusSerializer fallbackSerializer = null;

                if (mainSerializer is BinaryFormatterDataBusSerializer)
                {
                    fallbackSerializer = new SystemJsonDataBusSerializer();
                }

                if (mainSerializer is SystemJsonDataBusSerializer)
                {
                    fallbackSerializer = new BinaryFormatterDataBusSerializer();
                }

                return new DataBusReceiveBehavior(
                    b.GetRequiredService<IDataBus>(),
                    new DataBusDeserializer(mainSerializer, fallbackSerializer),
                    conventions);
            }));

            context.Pipeline.Register(new DataBusSendBehavior.Registration(conventions));
        }

        internal static string SelectedDataBusKey = "SelectedDataBus";
        internal static string CustomDataBusTypeKey = "CustomDataBusType";
        internal static string DataBusSerializerTypeKey = "DataBusSerializerType";

        class DataBusInitializer : FeatureStartupTask
        {
            IDataBus dataBus;

            public DataBusInitializer(IDataBus dataBus)
            {
                this.dataBus = dataBus;
            }

            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                return dataBus.Start(cancellationToken);
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
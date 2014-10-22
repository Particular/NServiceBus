namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using NServiceBus.DataBus;

    /// <summary>
    /// 
    /// </summary>
    public class DataBusCore : Feature
    {
        internal DataBusCore()
        {
            EnableByDefault();

            Prerequisite(DataBusPropertiesFound, "No databus properties was found in available messages");

            RegisterStartupTask<IDataBusInitializer>();
        }

        class IDataBusInitializer : FeatureStartupTask
        {
            public IDataBus DataBus { get; set; }

            protected override void OnStart()
            {
                DataBus.Start();
            }
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Container.HasComponent<IDataBusSerializer>())
            {
                context.Container.ConfigureComponent<DefaultDataBusSerializer>(DependencyLifecycle.SingleInstance);
            }

            context.Pipeline.Register<DataBusReceiveBehavior.Registration>();
            context.Pipeline.Register<DataBusSendBehavior.Registration>();
        }

        static bool DataBusPropertiesFound(FeatureConfigurationContext context)
        {
            var dataBusPropertyFound = false;
            var conventions = context.Settings.Get<Conventions>();

            if (!context.Container.HasComponent<IDataBusSerializer>() && System.Diagnostics.Debugger.IsAttached)
            {
                var properties = context.Settings.GetAvailableTypes()
                    .Where(conventions.IsMessageType)
                    .SelectMany(messageType => messageType.GetProperties())
                    .Where(conventions.IsDataBusProperty);

                foreach (var property in properties)
                {
                    dataBusPropertyFound = true;

                    if (!property.PropertyType.IsSerializable)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                @"The property type for '{0}' is not serializable. 
In order to use the databus feature for transporting the data stored in the property types defined in the call '.DefiningDataBusPropertiesAs()', need to be serializable. 
To fix this, please mark the property type '{0}' as serializable, see http://msdn.microsoft.com/en-us/library/system.runtime.serialization.iserializable.aspx on how to do this.",
                                String.Format("{0}.{1}", property.DeclaringType.FullName, property.Name)));
                    }
                }
            }
            else
            {
                dataBusPropertyFound = context.Settings.GetAvailableTypes()
                    .Where(conventions.IsMessageType)
                    .SelectMany(messageType => messageType.GetProperties())
                    .Any(conventions.IsDataBusProperty);
            }

            return dataBusPropertyFound;
        }
    }
}
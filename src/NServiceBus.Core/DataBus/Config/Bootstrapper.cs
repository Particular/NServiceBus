namespace NServiceBus.DataBus.Config
{
    using System;
    using System.Linq;
    using NServiceBus.Config;

    public class Bootstrapper : IWantToRunBeforeConfigurationIsFinalized, IWantToRunWhenConfigurationIsComplete
	{
        static bool dataBusPropertyFound;

        void IWantToRunBeforeConfigurationIsFinalized.Run()
		{
            if (!Configure.Instance.Configurer.HasComponent<IDataBusSerializer>() && System.Diagnostics.Debugger.IsAttached)
            {
                var properties = Configure.TypesToScan
                    .Where(MessageConventionExtensions.IsMessageType)
                    .SelectMany(messageType => messageType.GetProperties())
                    .Where(MessageConventionExtensions.IsDataBusProperty);

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
                dataBusPropertyFound = Configure.TypesToScan
                    .Where(MessageConventionExtensions.IsMessageType)
                    .SelectMany(messageType => messageType.GetProperties())
                    .Any(MessageConventionExtensions.IsDataBusProperty);
            }

		    if (!dataBusPropertyFound)
		    {
		        return;
		    }

			if (!Configure.Instance.Configurer.HasComponent<IDataBus>())
			{
			    throw new InvalidOperationException("Messages containing databus properties found, please configure a databus!");
			}

			Configure.Instance.Configurer.ConfigureComponent<DataBusMessageMutator>(
                DependencyLifecycle.InstancePerCall);

            if (!Configure.Instance.Configurer.HasComponent<IDataBusSerializer>())
            {
                Configure.Instance.Configurer.ConfigureComponent<DefaultDataBusSerializer>(
                    DependencyLifecycle.SingleInstance);
            }
		}

        void IWantToRunWhenConfigurationIsComplete.Run()
        {
            if (dataBusPropertyFound)
            {
                Bus.Started += (sender, eventArgs) => Configure.Instance.Builder.Build<IDataBus>().Start();
            }
        }

        public IStartableBus Bus { get; set; }
	}
}

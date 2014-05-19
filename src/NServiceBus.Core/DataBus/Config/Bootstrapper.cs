namespace NServiceBus.DataBus.Config
{
    using System;
    using System.Linq;

    class Bootstrapper : IWantToRunBeforeConfigurationIsFinalized, IWantToRunWhenBusStartsAndStops
	{
        static bool dataBusPropertyFound;

        void IWantToRunBeforeConfigurationIsFinalized.Run(Configure config)
		{
            if (!config.Configurer.HasComponent<IDataBusSerializer>() && System.Diagnostics.Debugger.IsAttached)
            {
                var properties = config.TypesToScan
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
                dataBusPropertyFound = config.TypesToScan
                    .Where(MessageConventionExtensions.IsMessageType)
                    .SelectMany(messageType => messageType.GetProperties())
                    .Any(MessageConventionExtensions.IsDataBusProperty);
            }

		    if (!dataBusPropertyFound)
		    {
		        return;
		    }

            if (!config.Configurer.HasComponent<IDataBus>())
			{
			    throw new InvalidOperationException("Messages containing databus properties found, please configure a databus!");
			}

            if (!config.Configurer.HasComponent<IDataBusSerializer>())
            {
                config.Configurer.ConfigureComponent<DefaultDataBusSerializer>(DependencyLifecycle.SingleInstance);
            }
		}

        public IDataBus DataBus { get; set; }

        public void Start()
        {
            if (DataBus != null)
            {
                DataBus.Start();    
            }
            
        }

        public void Stop()
        {
        }
	}
}

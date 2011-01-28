using System.Linq;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.DataBus.Config
{
	using System;
	using System.Configuration;

	public class Bootstrapper:INeedInitialization
    {
        public void Init()
        {
        	bool dataBusPropertyFound = Configure.TypesToScan
        		.Where(t => typeof (IMessage).IsAssignableFrom(t))
        		.SelectMany(messageType => messageType.GetProperties())
				.Any(t => typeof(IDataBusProperty).IsAssignableFrom(t.PropertyType));

            if (dataBusPropertyFound)
            {
				if(!Configure.Instance.Configurer.HasComponent<IDataBus>())
					throw new InvalidOperationException("Messages containing databus properties found, please configure a databus!");

				Configure.Instance.Configurer.ConfigureComponent<DataBusMessageMutator>(
					DependencyLifecycle.InstancePerCall);

				Configure.Instance.Configurer.ConfigureComponent<DefaultDatabusSerializer>(
					DependencyLifecycle.SingleInstance);


            	
            }
   
		}
    }
}
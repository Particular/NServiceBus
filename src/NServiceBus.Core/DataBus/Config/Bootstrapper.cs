namespace NServiceBus.DataBus.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NServiceBus.Config;

    public class Bootstrapper : NServiceBus.INeedInitialization, IWantToRunWhenConfigurationIsComplete
	{
		public void Init()
		{
            // check data bus
            if (!Configure.Instance.Configurer.HasComponent<IDataBus>())
                throw new InvalidOperationException("Messages containing databus properties found, please configure a databus!");
            if (!Configure.Instance.Configurer.HasComponent<IDataBusSerializer>())
                throw new InvalidOperationException("Messages containing databus properties found, please configure a databus serializer!");

            // check properties
            IEnumerable<PropertyInfo> properties = Configure.TypesToScan
                .Where(MessageConventionExtensions.IsMessageType)
                .SelectMany(messageType => messageType.GetProperties())
                .Where(MessageConventionExtensions.IsDataBusProperty);
            dataBusPropertyFound = properties.Any();
            Configure.Instance.Builder.Build<IDataBusSerializer>().Validate(properties);

            // register mutator
            if (!dataBusPropertyFound)
                return;
            Configure.Instance.Configurer.ConfigureComponent<DataBusMessageMutator>(DependencyLifecycle.InstancePerCall);
		}

	    public void Run()
	    {
            if (!dataBusPropertyFound)
                return;

            Bus.Started += (sender, eventargs) => Configure.Instance.Builder.Build<IDataBus>().Start();
	    }

        public IStartableBus Bus { get; set; }

	    private static bool dataBusPropertyFound;
	}
}

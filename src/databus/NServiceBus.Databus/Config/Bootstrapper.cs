using System;
using System.Linq;
using NServiceBus.Config;

namespace NServiceBus.DataBus.Config
{
	public class Bootstrapper : INeedInitialization, IWantToRunWhenConfigurationIsComplete
	{
		public void Init()
		{
		    dataBusPropertyFound = Configure.TypesToScan
		        .Where(t => t.IsMessageType())
				.SelectMany(messageType => messageType.GetProperties())
				.Any(t => typeof(IDataBusProperty).IsAssignableFrom(t.PropertyType));

			if (!dataBusPropertyFound)
				return;

			if (!Configure.Instance.Configurer.HasComponent<IDataBus>())
				throw new InvalidOperationException("Messages containing databus properties found, please configure a databus!");

			Configure.Instance.Configurer.ConfigureComponent<DataBusMessageMutator>(
				DependencyLifecycle.InstancePerCall);

			Configure.Instance.Configurer.ConfigureComponent<DefaultDataBusSerializer>(
				DependencyLifecycle.SingleInstance);
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
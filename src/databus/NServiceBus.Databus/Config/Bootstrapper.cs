using System.Linq;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.DataBus.Config
{
	using System;
	using System.Configuration;
	using Unicast;

	public class Bootstrapper : INeedInitialization
	{
		public void Init()
		{
			bool dataBusPropertyFound = Configure.TypesToScan
				.Where(t => typeof(IMessage).IsAssignableFrom(t))
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

			HookupDataBusStartMethod();
		}

		static void HookupDataBusStartMethod()
		{
			Configure.ConfigurationComplete +=
				(o,a) =>
					{
						Configure.Instance.Builder.Build<IStartableBus>()
							.Started += (sender, eventargs) => Configure.Instance.Builder.Build<IDataBus>().Start();

					};
		}
	}
}
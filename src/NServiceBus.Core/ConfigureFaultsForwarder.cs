namespace NServiceBus
{
    using Faults;
    using Faults.Forwarder;
    using Settings;

    /// <summary>
	/// Contains extension methods to NServiceBus.Configure
	/// </summary>
	public static class ConfigureFaultsForwarder
	{
		/// <summary>
		/// Forward messages that have repeatedly failed to another endpoint.
		/// </summary>
		public static Configure MessageForwardingInCaseOfFault(this Configure config)
		{
			if (ErrorQueue != null)
			{
				 return config;
			}
			if (SettingsHolder.Get<bool>("Endpoint.SendOnly"))
			{
				return config;
			}

		    ErrorQueue = config.GetConfiguredErrorQueue();
			config.Configurer.ConfigureComponent<FaultManager>(DependencyLifecycle.InstancePerCall)
				.ConfigureProperty(fm => fm.ErrorQueue, ErrorQueue);

			return config;
		}

		/// <summary>
		/// The queue to which to forward errors.
		/// </summary>
		public static Address ErrorQueue { get; private set; }

	}

	class Bootstrapper : INeedInitialization
	{
		public void Init()
		{
			if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
			{
				Configure.Instance.MessageForwardingInCaseOfFault();
			}
		}
	}
}

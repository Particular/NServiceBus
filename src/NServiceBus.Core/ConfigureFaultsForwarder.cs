namespace NServiceBus
{
    using System.Configuration;
    using Config;
    using Faults;
    using Faults.Forwarder;
    using Logging;
    using Utils;

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
			if (config.Settings.Get<bool>("Endpoint.SendOnly"))
			{
				return config;
			}

			ErrorQueue = Address.Undefined;

			var section = config.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
			if (section != null)
			{
				if (string.IsNullOrWhiteSpace(section.ErrorQueue))
				{
					throw new ConfigurationErrorsException(
						"'MessageForwardingInCaseOfFaultConfig' configuration section is found but 'ErrorQueue' value is missing." +
						"\n The following is an example for adding such a value to your app config: " +
						"\n <MessageForwardingInCaseOfFaultConfig ErrorQueue=\"error\"/> \n");
				}

				Logger.Debug("Error queue retrieved from <MessageForwardingInCaseOfFaultConfig> element in config file.");

				ErrorQueue = Address.Parse(section.ErrorQueue);

				config.Configurer.ConfigureComponent<FaultManager>(DependencyLifecycle.InstancePerCall)
					.ConfigureProperty(fm => fm.ErrorQueue, ErrorQueue);

				return config;
			}

			
			var errorQueue = RegistryReader<string>.Read("ErrorQueue");
			if (!string.IsNullOrWhiteSpace(errorQueue))
			{
				Logger.Debug("Error queue retrieved from registry settings.");
				ErrorQueue = Address.Parse(errorQueue);

				config.Configurer.ConfigureComponent<FaultManager>(DependencyLifecycle.InstancePerCall)
					.ConfigureProperty(fm => fm.ErrorQueue, ErrorQueue);
			}
			
			if (ErrorQueue == Address.Undefined)
			{
				throw new ConfigurationErrorsException("Faults forwarding requires an error queue to be specified. Please add a 'MessageForwardingInCaseOfFaultConfig' section to your app.config" +
                "\n or configure a global one using the powershell command: Set-NServiceBusLocalMachineSettings -ErrorQueue {address of error queue}");
			}

			return config;
		}

		/// <summary>
		/// The queue to which to forward errors.
		/// </summary>
		public static Address ErrorQueue { get; private set; }

		static readonly ILog Logger = LogManager.GetLogger(typeof(ConfigureFaultsForwarder));
	}

	class Bootstrapper : INeedInitialization
	{
        public void Init(Configure config)
		{
            if (!config.Configurer.HasComponent<IManageMessageFailures>())
			{
                config.MessageForwardingInCaseOfFault();
			}
		}
	}
}

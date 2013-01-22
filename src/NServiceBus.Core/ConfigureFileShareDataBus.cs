namespace NServiceBus
{
    using DataBus;
    using DataBus.FileShare;

    /// <summary>
	/// Contains extension methods to NServiceBus.Configure for the file share data bus
	/// </summary>
	public static class ConfigureFileShareDataBus
	{
		/// <summary>
		/// Use the file-based databus implementation with the default binary serializer.
		/// </summary>
		/// <param name="config">The fluent configuration.</param>
		/// <param name="basePath">The location to which to write serialized properties for the databus.</param>
		public static Configure FileShareDataBus(this Configure config,string basePath)
		{
			var dataBus = new FileShareDataBus(basePath);

			config.Configurer.RegisterSingleton<IDataBus>(dataBus);
			config.Configurer.ConfigureComponent<DefaultDataBusSerializer>(DependencyLifecycle.SingleInstance);

			return config;
		}
	}

}
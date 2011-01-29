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
		/// Use the in memory saga persister implementation.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="basePath">Path to file share to store the data on</param>
		/// <returns></returns>
		public static Configure FileShareDataBus(this Configure config,string basePath)
		{
			var dataBus = new FileShareDataBus(basePath);

			config.Configurer.RegisterSingleton<IDataBus>(dataBus);

			return config;
		}
	}

}
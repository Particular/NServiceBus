namespace NServiceBus
{
    using Config;
    using DataBus;
    using DataBus.Azure.BlobStorage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using CloudStorageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount;

    /// <summary>
	/// Contains extension methods to NServiceBus.Configure for the azure blob storage data bus
	/// </summary>
	public static class ConfigureAzureBlobStorageDataBus
	{
	    public const string Defaultcontainer = "databus";
        public const string DefaultBasePath = "";
        public const int DefaultMaxRetries = 5;
        public const int DefaultNumberOfIOThreads = 5;
	    public const string DefaultConnectionString = "UseDevelopmentStorage=true";
	    public const int DefaultBlockSize = 4*1024*1024; // 4MB
		
		public static Configure AzureDataBus(this Configure config)
		{
            var container = Defaultcontainer;
            
		    CloudBlobClient cloudBlobClient;

            var configSection = Configure.GetConfigSection<AzureDataBusConfig>();

            if (configSection != null)
            {
                cloudBlobClient = CloudStorageAccount.Parse(configSection.ConnectionString).CreateCloudBlobClient();

                container = configSection.Container;
            }
            else
            {
                cloudBlobClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudBlobClient();
            }

            var dataBus = new BlobStorageDataBus(cloudBlobClient.GetContainerReference(container));

            if(configSection != null)
            {
                dataBus.BasePath = configSection.BasePath;
                dataBus.MaxRetries = configSection.MaxRetries;
                dataBus.NumberOfIOThreads = configSection.NumberOfIOThreads;
                dataBus.BlockSize = configSection.BlockSize;
            }

		    config.Configurer.RegisterSingleton<IDataBus>(dataBus);

			return config;
		}
	}
}
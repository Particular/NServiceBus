using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using NServiceBus.DataBus;
using NServiceBus.DataBus.Azure.BlobStorage;
using NServiceBus.Config;

namespace NServiceBus
{
	/// <summary>
	/// Contains extension methods to NServiceBus.Configure for the azure blob storage data bus
	/// </summary>
	public static class ConfigureAzureBlobStorageDataBus
	{
	    public const string Defaultcontainer = "$root";
        public const string DefaultBasePath = "";
        public const int DefaultMaxRetries = 5;
        public const int DefaultNumberOfIOThreads = 5;
	    public const string DefaultConnectionString = "UseDevelopmentStorage=true";
		
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
            }

		    config.Configurer.RegisterSingleton<IDataBus>(dataBus);

			return config;
		}
	}
}
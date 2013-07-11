namespace NServiceBus.Transports.StorageQueues
{
    using System;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Transports;

    /// <summary>
    /// Creates the queues. Note that this class will only be invoked when running the windows host and not when running in the fabric
    /// </summary>
    public class AzureMessageQueueCreator : ICreateQueues
    {
        public CloudQueueClient Client { get; set; }

        public void CreateQueueIfNecessary(Address address, string account)
        {
            var queueName = AzureMessageQueueUtils.GetQueueName(address);

            try
            {
                var queue = Client.GetQueueReference(queueName);

                queue.CreateIfNotExists();

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create queue: " + queueName,ex);
            }
        }
    }
}
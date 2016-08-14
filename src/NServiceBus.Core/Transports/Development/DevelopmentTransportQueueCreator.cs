namespace NServiceBus
{
    using System.IO;
    using System.Threading.Tasks;
    using Transport;

    class DevelopmentTransportQueueCreator : ICreateQueues
    {
        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            foreach (var address in queueBindings.SendingAddresses)
            {
                CreateQueueFolder(address);
            }

            foreach (var address in queueBindings.ReceivingAddresses)
            {
                CreateQueueFolder(address);
            }

            return TaskEx.CompletedTask;
        }

        static void CreateQueueFolder(string address)
        {
            var fullPath = Path.Combine("c:\\bus", address);
            Directory.CreateDirectory(fullPath);
            Directory.CreateDirectory(Path.Combine(fullPath, ".committed"));
            Directory.CreateDirectory(Path.Combine(fullPath, ".bodies"));
        }
    }
}
using System;
using NServiceBus;
using NServiceBus.Gateway.Persistence;

namespace SiteB
{
    public class DeduplicationCleanup : IWantToRunWhenBusStartsAndStops
    {
        public InMemoryPersistence MemoryPersistence { get; set; }
        public void Start()
        {
            Schedule.Every(TimeSpan.FromMinutes(1))
                //delete all ID's older than 5 minutes
                .Action(() =>
                {
                    var numberOfDeletedMessages =
                        MemoryPersistence.DeleteDeliveredMessages(DateTime.UtcNow.AddMinutes(-5));

                    Console.Out.WriteLine("InMemory store cleared, number of items deleted: {0}", numberOfDeletedMessages);
                });
        }

        public void Stop()
        {
        }
    }
}
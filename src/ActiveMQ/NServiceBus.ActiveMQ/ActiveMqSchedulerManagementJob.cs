namespace NServiceBus.Transports.ActiveMQ
{
    using System;
    using Apache.NMS;

    public class ActiveMqSchedulerManagementJob
    {
        public ActiveMqSchedulerManagementJob(IMessageConsumer consumer, IDestination temporaryDestination, DateTime expirationDate)
        {
            Consumer = consumer;
            Destination = temporaryDestination;
            ExprirationDate = expirationDate;
        }

        public IMessageConsumer Consumer { get; set; }
        public IDestination Destination { get; set; }
        public DateTime ExprirationDate { get; set; }
    }
}
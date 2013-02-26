namespace NServiceBus.Transports.ActiveMQ
{
    using System;
    using Apache.NMS;

    public class ActiveMqSchedulerManagementJob
    {
        public ActiveMqSchedulerManagementJob(IMessageConsumer consumer, IDestination temporaryDestination, DateTime expirationDate)
        {
            this.Consumer = consumer;
            this.Destination = temporaryDestination;
            this.ExprirationDate = expirationDate;
        }

        public IMessageConsumer Consumer { get; set; }
        public IDestination Destination { get; set; }
        public DateTime ExprirationDate { get; set; }
    }
}
namespace NServiceBus.Transports.ActiveMQ
{
    using System;
    using System.Transactions;
    using Apache.NMS;
    using NServiceBus.Support;
    using SessionFactories;

    public class ActiveMqSchedulerManagementCommands : IActiveMqSchedulerManagementCommands
    {
        private readonly TimeSpan DeleteTaskMaxIdleTime = TimeSpan.FromSeconds(10);
        private ISession consumerSession;
        public ISessionFactory SessionFactory { get; set; }

        public void Start()
        {
            this.consumerSession = this.SessionFactory.GetSession();
        }

        public void Stop()
        {
            this.SessionFactory.Release(this.consumerSession);
        }
        
        public void RequestDeferredMessages(IDestination browseDestination)
        {
            var session = this.SessionFactory.GetSession();
            var amqSchedulerManagementDestionation =
                session.GetTopic(ScheduledMessage.AMQ_SCHEDULER_MANAGEMENT_DESTINATION);

            using (var producer = session.CreateProducer(amqSchedulerManagementDestionation))
            {
                var request = session.CreateMessage();
                request.Properties[ScheduledMessage.AMQ_SCHEDULER_ACTION] = ScheduledMessage.AMQ_SCHEDULER_ACTION_BROWSE;
                request.NMSReplyTo = browseDestination;
                producer.Send(request);
            }
        }

        public ActiveMqSchedulerManagementJob CreateActiveMqSchedulerManagementJob(string selector)
        {
            var temporaryDestination = this.consumerSession.CreateTemporaryTopic();
            var consumer = this.consumerSession.CreateConsumer(
                temporaryDestination,
                selector);
            return new ActiveMqSchedulerManagementJob(consumer, temporaryDestination, SystemClock.TechnicalTime + this.DeleteTaskMaxIdleTime);        
        }

        public void DisposeJob(ActiveMqSchedulerManagementJob job)
        {
            job.Consumer.Dispose();
            this.consumerSession.DeleteDestination(job.Destination);
        }

        public void ProcessJob(ActiveMqSchedulerManagementJob job)
        {
            IMessage message = job.Consumer.ReceiveNoWait();
            while (message != null)
            {
                this.RemoveDeferredMessages(message.Properties[ScheduledMessage.AMQ_SCHEDULED_ID]);

                job.ExprirationDate = SystemClock.TechnicalTime + this.DeleteTaskMaxIdleTime;
                message = job.Consumer.ReceiveNoWait();
            }
        }

        private void RemoveDeferredMessages(object id)
        {
            using (var tx = new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var producer = this.consumerSession.CreateProducer(this.consumerSession.GetTopic(ScheduledMessage.AMQ_SCHEDULER_MANAGEMENT_DESTINATION)))
                {
                    var remove = this.consumerSession.CreateMessage();
                    remove.Properties[ScheduledMessage.AMQ_SCHEDULER_ACTION] = ScheduledMessage.AMQ_SCHEDULER_ACTION_REMOVE;
                    remove.Properties[ScheduledMessage.AMQ_SCHEDULED_ID] = id;
                    producer.Send(remove);
                }

                tx.Complete();
            }
        }
    }
}
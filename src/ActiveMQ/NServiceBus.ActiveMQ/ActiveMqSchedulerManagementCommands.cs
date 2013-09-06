namespace NServiceBus.Transports.ActiveMQ
{
    using System;
    using System.Transactions;
    using Apache.NMS;
    using SessionFactories;

    public class ActiveMqSchedulerManagementCommands : IActiveMqSchedulerManagementCommands
    {
        private readonly TimeSpan DeleteTaskMaxIdleTime = TimeSpan.FromSeconds(10);
        private ISession consumerSession;
        public ISessionFactory SessionFactory { get; set; }

        public void Start()
        {
            consumerSession = SessionFactory.GetSession();
        }

        public void Stop()
        {
            SessionFactory.Release(consumerSession);
        }
        
        public void RequestDeferredMessages(IDestination browseDestination)
        {
            var session = SessionFactory.GetSession();
            var amqSchedulerManagementDestination =
                session.GetTopic(ScheduledMessage.AMQ_SCHEDULER_MANAGEMENT_DESTINATION);

            using (var producer = session.CreateProducer(amqSchedulerManagementDestination))
            {
                var request = session.CreateMessage();
                request.Properties[ScheduledMessage.AMQ_SCHEDULER_ACTION] = ScheduledMessage.AMQ_SCHEDULER_ACTION_BROWSE;
                request.NMSReplyTo = browseDestination;
                producer.Send(request);
            }
        }

        public ActiveMqSchedulerManagementJob CreateActiveMqSchedulerManagementJob(string selector)
        {
            var temporaryDestination = consumerSession.CreateTemporaryTopic();
            var consumer = consumerSession.CreateConsumer(
                temporaryDestination,
                selector);
            return new ActiveMqSchedulerManagementJob(consumer, temporaryDestination, DateTime.Now + DeleteTaskMaxIdleTime);        
        }

        public void DisposeJob(ActiveMqSchedulerManagementJob job)
        {
            job.Consumer.Dispose();
            consumerSession.DeleteDestination(job.Destination);
        }

        public void ProcessJob(ActiveMqSchedulerManagementJob job)
        {
            var message = job.Consumer.ReceiveNoWait();
            while (message != null)
            {
                RemoveDeferredMessages(message.Properties[ScheduledMessage.AMQ_SCHEDULED_ID]);

                job.ExprirationDate = DateTime.Now + DeleteTaskMaxIdleTime;
                message = job.Consumer.ReceiveNoWait();
            }
        }

        private void RemoveDeferredMessages(object id)
        {
            using (var tx = new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var producer = consumerSession.CreateProducer(consumerSession.GetTopic(ScheduledMessage.AMQ_SCHEDULER_MANAGEMENT_DESTINATION)))
                {
                    var remove = consumerSession.CreateMessage();
                    remove.Properties[ScheduledMessage.AMQ_SCHEDULER_ACTION] = ScheduledMessage.AMQ_SCHEDULER_ACTION_REMOVE;
                    remove.Properties[ScheduledMessage.AMQ_SCHEDULED_ID] = id;
                    producer.Send(remove);
                }

                tx.Complete();
            }
        }
    }
}
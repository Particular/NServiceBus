namespace NServiceBus.Deduplication.NHibernate
{
    using System;
    using System.Data;
    using Config;
    using Gateway.Deduplication;
    using global::NHibernate;
    using global::NHibernate.Exceptions;

    /// <summary>
    /// NHibernate Gateway deduplication
    /// </summary>
    public class GatewayDeduplication : IDeduplicateMessages
    {
        /// <summary>
        /// Creates <c>ISession</c>s.
        /// </summary>
        public ISessionFactory SessionFactory { get; set; }

        /// <summary>
        /// Adds a new message
        /// </summary>
        /// <param name="clientId">Client to add</param>
        /// <param name="timeReceived">Time the message was received</param>
        /// <returns><value>true</value> if successfully added.</returns>
        public bool DeduplicateMessage(string clientId, DateTime timeReceived)
        {
            using (var session = SessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var gatewayMessage = session.Get<DeduplicationMessage>(clientId);

                if (gatewayMessage != null)
                {
                    tx.Commit();
                    return false;
                }

                gatewayMessage = new DeduplicationMessage
                {
                    Id = clientId,
                    TimeReceived = timeReceived
                };

                try
                {
                    session.Save(gatewayMessage);
                    tx.Commit();
                }
                catch (GenericADOException)
                {
                    tx.Rollback();
                    return false;
                }
            }

            return true;
        }
    }
}

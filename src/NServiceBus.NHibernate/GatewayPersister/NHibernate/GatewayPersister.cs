namespace NServiceBus.GatewayPersister.NHibernate
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using Config;
    using Gateway.Persistence;
    using global::NHibernate;
    using global::NHibernate.Exceptions;
    using Serializers.Json;

    /// <summary>
    /// NHibernate Gateway persister;
    /// </summary>
    public class GatewayPersister : IPersistMessages
    {
        /// <summary>
        /// Creates <c>ISession</c>s.
        /// </summary>
        public ISessionFactory SessionFactory { get; set; }

        /// <summary>
        /// Adds a new message.
        /// </summary>
        /// <param name="clientId">Client to add.</param>
        /// <param name="timeReceived">Time the message was received</param>
        /// <param name="message">The original message.</param>
        /// <param name="headers">The headers.</param>
        /// <returns><value>true</value> if successfully added.</returns>
        public bool InsertMessage(string clientId, DateTime timeReceived, Stream message, IDictionary<string, string> headers)
        {
            using (var session = SessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var gatewayMessage = session.Get<GatewayMessage>(clientId);

                if (gatewayMessage != null)
                {
                    tx.Commit();
                    return false;
                }

                gatewayMessage = new GatewayMessage
                                         {
                                             Id = clientId,
                                             Headers = ConvertDictionaryToString(headers),
                                             TimeReceived = timeReceived,
                                             OriginalMessage = new byte[message.Length],
                                         };

                message.Read(gatewayMessage.OriginalMessage, 0, (int) message.Length);
                

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

        /// <summary>
        /// Update the acknowledged flag.
        /// </summary>
        /// <param name="clientId">Client to update.</param>
        /// <param name="message">The original message.</param>
        /// <param name="headers">The headers.</param>
        /// <returns><value>true</value> if successfully updated.</returns>
        public bool AckMessage(string clientId, out byte[] message, out IDictionary<string, string> headers)
        {
            message = null;
            headers = null;

            using (var session = SessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var gatewayMessage = session.Get<GatewayMessage>(clientId);

                if (gatewayMessage == null)
                {
                    tx.Commit();
                    throw new InvalidOperationException(string.Format("No message with id: {0} found.", clientId));
                }

                if (gatewayMessage.Acknowledged)
                {
                    tx.Commit();
                    return false;
                }

                message = gatewayMessage.OriginalMessage;
                headers = ConvertStringToDictionary(gatewayMessage.Headers);

                gatewayMessage.Acknowledged = true;

                tx.Commit(); 
            }

            return true;
        }

        /// <summary>
        /// Updates the header value.
        /// </summary>
        /// <param name="clientId">Client to update the header value for.</param>
        /// <param name="headerKey">Header key.</param>
        /// <param name="newValue">New value.</param>
        public void UpdateHeader(string clientId, string headerKey, string newValue)
        {
            using (var session = SessionFactory.OpenSession())
            using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var gatewayMessage = session.Get<GatewayMessage>(clientId);

                if (gatewayMessage == null)
                {
                    tx.Commit();
                    throw new InvalidOperationException(string.Format("No message with id: {0} found.", clientId));
                }

                var headers = ConvertStringToDictionary(gatewayMessage.Headers);
                headers[headerKey] = newValue;

                gatewayMessage.Headers = ConvertDictionaryToString(headers);

                tx.Commit();
            }
        }

        static Dictionary<string, string> ConvertStringToDictionary(string data)
        {
            if (String.IsNullOrEmpty(data))
            {
                return new Dictionary<string, string>();
            }

            return Serializer.DeserializeObject<Dictionary<string, string>>(data);
        }

        static string ConvertDictionaryToString(ICollection<KeyValuePair<string, string>> data)
        {
            if (data == null || data.Count == 0)
            {
                return null;
            }

            return Serializer.SerializeObject(data);
        }

        static readonly JsonMessageSerializer Serializer = new JsonMessageSerializer(null);
    }
}

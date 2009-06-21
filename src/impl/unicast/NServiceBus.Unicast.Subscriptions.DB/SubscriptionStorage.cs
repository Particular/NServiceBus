using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using NServiceBus.Unicast.Transport;
using System.Text;

namespace NServiceBus.Unicast.Subscriptions.DB
{
    /// <summary>
    /// Database-backed implementation of <see cref="ISubscriptionStorage" />.
    /// </summary>
    public class SubscriptionStorage : ISubscriptionStorage
    {
        /// <summary>
        /// Constructor setting default isolational level to ReadCommitted.
        /// </summary>
        public SubscriptionStorage()
        {
            IsolationLevel = IsolationLevel.ReadCommitted;
        }

        #region members

        private DbProviderFactory factory;

        private readonly IDictionary<Type, IList<Type>> compatibleTypes = new Dictionary<Type, IList<Type>>();

        #endregion

        #region config

        /// <summary>
        /// The database table which will store the subscription data.
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// The name of the column that will hold the message type.
        /// </summary>
        public string MessageTypeColumnName { get; set; }

        /// <summary>
        /// The name of the column that will hold the subscriber address.
        /// </summary>
        public string SubscriberColumnName { get; set; }

        /// <summary>
        /// The isolation level to perform transactions with.
        /// Default value is ReadCommitted.
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; }

        /// <summary>
        /// The name of the database provider.
        /// </summary>
        public string ProviderInvariantName { get; set; }

        /// <summary>
        /// The connection string to the database.
        /// </summary>
        public string ConnectionString { get; set; }

        #endregion

        #region ISubscriptionStorage Members

        /// <summary>
        /// Returns the list of subscriber addresses subscribed for the given message type.
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public IList<string> GetSubscribersForMessage(Type messageType)
        {
            var compatibles = new List<Type> {messageType};
            if (compatibleTypes.ContainsKey(messageType))
                compatibles.AddRange(compatibleTypes[messageType]);

            var result = new List<string>();

            var command = GetConnection().CreateCommand();
            command.CommandType = CommandType.Text;

            var builder = new StringBuilder("SELECT {0} FROM {1} WHERE ");
            for (int i = 0; i < compatibles.Count; i++)
            {
                string paramName = "@" + MessageTypeColumnName + i;

                DbParameter msgParam = command.CreateParameter();
                msgParam.ParameterName = paramName;
                msgParam.Value = compatibles[i].AssemblyQualifiedName;
                command.Parameters.Add(msgParam);

                builder.Append("{2}=");
                builder.Append(paramName);

                if (i != compatibles.Count - 1)
                    builder.Append(" OR ");
            }

            command.CommandText =
                string.Format(
                    builder.ToString(),
                    SubscriberColumnName, Table, MessageTypeColumnName);

            using (command.Connection)
            {
                DbDataReader reader = command.ExecuteReader();
                while (reader.Read())
                    result.Add(reader.GetString(0));

                reader.Close();
            }

            return result;
        }

        /// <summary>
        /// Handles subscription messages.
        /// </summary>
        /// <param name="msg"></param>
        public void HandleSubscriptionMessage(TransportMessage msg)
        {
            IMessage[] messages = msg.Body;
            if (messages == null)
                return;

            if (messages.Length != 1)
                return;

            var subMessage = messages[0] as SubscriptionMessage;
            if (subMessage == null)
                return;

            using (DbConnection connection = GetConnection())
            using (DbTransaction tx = connection.BeginTransaction(IsolationLevel))
            {
                if (subMessage.SubscriptionType == SubscriptionType.Add)
                    Execute(
                        tx,
                        string.Format(
                        "SET NOCOUNT ON;" +
                        "INSERT INTO {0} ({1}, {2}) " +
                        "(SELECT @{1} AS {1}, @{2} AS {2} WHERE (NOT EXISTS " +
                        "(SELECT {1} FROM {0} AS S2 WHERE ({1} = @{1}) AND ({2} = @{2}))))",
                                      Table, SubscriberColumnName, MessageTypeColumnName),
                        msg.ReturnAddress,
                        subMessage.TypeName
                        );

                if (subMessage.SubscriptionType == SubscriptionType.Remove)
                    Execute(
                        tx,
                        string.Format("SET NOCOUNT ON; DELETE FROM {0} WHERE {1}=@{1} AND {2}=@{2}",
                                      Table, SubscriberColumnName, MessageTypeColumnName),
                        msg.ReturnAddress,
                        subMessage.TypeName
                        );

                tx.Commit();
            }
        }

        /// <summary>
        /// Initializes the storage.
        /// Scans the given message types to find compatibilities between them
        /// based on inheritance.
        /// </summary>
        /// <param name="messageTypes"></param>
        public void Init(IList<Type> messageTypes)
        {
            if (ConnectionString == null ||
                ProviderInvariantName == null ||
                MessageTypeColumnName == null ||
                SubscriberColumnName == null ||
                Table == null)
                throw new ConfigurationErrorsException(
                    "ConnectionString, MessageTypeParameterName, SubscriberParameterName, Table, or ProviderInvariantName have not been set.");

            factory = DbProviderFactories.GetFactory(ProviderInvariantName);

            foreach (Type msgType in messageTypes)
            {
                ScanBase(msgType);

                foreach (Type interfaceType in msgType.GetInterfaces())
                    if (typeof (IMessage).IsAssignableFrom(interfaceType) && typeof(IMessage) != interfaceType)
                        RegisterMapping(msgType, interfaceType);
            }
        }

        private void ScanBase(Type msgType)
        {
            if (msgType == null)
                return;

            Type baseType = msgType.BaseType;
            if (typeof(IMessage).IsAssignableFrom(baseType))
                RegisterMapping(msgType, baseType);

            ScanBase(baseType);
        }

        private void RegisterMapping(Type specific, Type generic)
        {
            IList<Type> genericTypes;
            compatibleTypes.TryGetValue(specific, out genericTypes);

            if (genericTypes == null)
            {
                genericTypes = new List<Type>();
                compatibleTypes[specific] = genericTypes;
            }

            genericTypes.Add(generic);
        }

        #endregion

        #region db helper

        private void Execute(DbTransaction tx, string sql, string subscriber, string messageType)
        {
            DbCommand command = tx.Connection.CreateCommand();
            command.Transaction = tx;
            command.CommandType = CommandType.Text;

            DbParameter subParam = command.CreateParameter();
            subParam.ParameterName = "@" + SubscriberColumnName;
            subParam.Value = subscriber;
            command.Parameters.Add(subParam);

            DbParameter msgParam = command.CreateParameter();
            msgParam.ParameterName = "@" + MessageTypeColumnName;
            msgParam.Value = messageType;
            command.Parameters.Add(msgParam);

            command.CommandText = sql;

            command.ExecuteNonQuery();
        }

        private DbConnection GetConnection()
        {
            DbConnection connection = factory.CreateConnection();

            connection.ConnectionString = ConnectionString;
            connection.Open();

            return connection;
        }

        #endregion
    }
}

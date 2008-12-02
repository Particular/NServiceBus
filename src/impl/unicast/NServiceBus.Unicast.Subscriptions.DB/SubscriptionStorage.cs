using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using NServiceBus.Unicast.Transport;
using System.Text;

namespace NServiceBus.Unicast.Subscriptions.DB
{
    public class SubscriptionStorage : ISubscriptionStorage
    {
        #region members

        private string table;
        private string messageTypeParameterName;
        private string subscriberParameterName;

        private string connectionString;
        private DbProviderFactory factory;
        private IsolationLevel isolationLevel = IsolationLevel.ReadCommitted;

        #endregion

        #region config

        public virtual string Table
        {
            set
            {
                this.table = value;
            }
        }

        public virtual string MessageTypeParameterName
        {
            set { this.messageTypeParameterName = value; }
        }

        public virtual string SubscriberParameterName
        {
            set { this.subscriberParameterName = value; }
        }

        public virtual IsolationLevel IsolationLevel
        {
            set
            {
                this.isolationLevel = value;
            }
        }

        public virtual string ProviderInvariantName
        {
            set
            {
                factory = DbProviderFactories.GetFactory(value);
            }
        }

        public virtual string ConnectionString
        {
            get { return connectionString; }
            set { connectionString = value; }
        }

        #endregion

        #region ISubscriptionStorage Members

        public IList<string> GetSubscribersForMessage(Type messageType)
        {
            List<Type> compatibles = new List<Type>();
            compatibles.Add(messageType);
            if (compatibleTypes.ContainsKey(messageType))
                compatibles.AddRange(compatibleTypes[messageType]);

            List<string> result = new List<string>();

            DbCommand command = this.GetConnection().CreateCommand();
            command.CommandType = CommandType.Text;

            StringBuilder builder = new StringBuilder("SELECT {0} FROM {1} WHERE ");
            for (int i = 0; i < compatibles.Count; i++)
            {
                string paramName = "@" + messageTypeParameterName + i;

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
                    subscriberParameterName, this.table, messageTypeParameterName);

            using (command.Connection)
            {
                DbDataReader reader = command.ExecuteReader();
                while (reader.Read())
                    result.Add(reader.GetString(0));

                reader.Close();
            }

            return result;
        }

        public void HandleSubscriptionMessage(TransportMessage msg)
        {
            IMessage[] messages = msg.Body;
            if (messages == null)
                return;

            if (messages.Length != 1)
                return;

            SubscriptionMessage subMessage = messages[0] as SubscriptionMessage;
            if (subMessage == null)
                return;

            using (DbConnection connection = GetConnection())
            using (DbTransaction tx = connection.BeginTransaction(isolationLevel))
            {
                if (subMessage.SubscriptionType == SubscriptionType.Add)
                    Execute(
                        tx,
                        string.Format(
                        "SET NOCOUNT ON;" +
                        "INSERT INTO {0} ({1}, {2}) " +
                        "(SELECT @{1} AS {1}, @{2} AS {2} WHERE (NOT EXISTS " +
                        "(SELECT {1} FROM {0} AS S2 WHERE ({1} = @{1}) AND ({2} = @{2}))))",
                                      this.table, subscriberParameterName, messageTypeParameterName),
                        msg.ReturnAddress,
                        subMessage.TypeName
                        );

                if (subMessage.SubscriptionType == SubscriptionType.Remove)
                    Execute(
                        tx,
                        string.Format("SET NOCOUNT ON; DELETE FROM {0} WHERE {1}=@{1} AND {2}=@{2}",
                                      this.table, subscriberParameterName, messageTypeParameterName),
                        msg.ReturnAddress,
                        subMessage.TypeName
                        );

                tx.Commit();
            }
        }

        public void Init(IList<Type> messageTypes)
        {
            if (this.connectionString == null ||
                this.factory == null ||
                this.messageTypeParameterName == null ||
                this.subscriberParameterName == null ||
                this.table == null)
                throw new ConfigurationErrorsException(
                    "ConnectionString, MessageTypeParameterName, SubscriberParameterName, Table, or ProviderInvariantName have not been set.");

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
            this.compatibleTypes.TryGetValue(specific, out genericTypes);

            if (genericTypes == null)
            {
                genericTypes = new List<Type>();
                this.compatibleTypes[specific] = genericTypes;
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
            subParam.ParameterName = "@" + subscriberParameterName;
            subParam.Value = subscriber;
            command.Parameters.Add(subParam);

            DbParameter msgParam = command.CreateParameter();
            msgParam.ParameterName = "@" + messageTypeParameterName;
            msgParam.Value = messageType;
            command.Parameters.Add(msgParam);

            command.CommandText = sql;

            command.ExecuteNonQuery();
        }

        private DbConnection GetConnection()
        {
            DbConnection connection = factory.CreateConnection();

            connection.ConnectionString = this.connectionString;
            connection.Open();

            return connection;
        }

        #endregion

        private IDictionary<Type, IList<Type>> compatibleTypes = new Dictionary<Type, IList<Type>>();
    }
}

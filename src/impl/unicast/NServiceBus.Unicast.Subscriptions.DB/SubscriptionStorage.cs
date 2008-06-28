using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using NServiceBus.Unicast.Transport;

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

        public IList<string> GetSubscribersForMessage(IMessage message)
        {
            List<string> result = new List<string>();
            string messageType = message.GetType().AssemblyQualifiedName;

            DbCommand command = this.GetConnection().CreateCommand();
            command.CommandType = CommandType.Text;

            DbParameter msgParam = command.CreateParameter();
            msgParam.ParameterName = "@" + messageTypeParameterName;
            msgParam.Value = messageType;
            command.Parameters.Add(msgParam);

            command.CommandText =
                string.Format(
                    "SELECT {0} FROM {1} WHERE {2}=@{2}", 
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

        public bool HandledSubscriptionMessage(TransportMessage msg)
        {
            IMessage[] messages = msg.Body;
            if (messages == null)
                return false;

            if (messages.Length != 1)
                return false;

            SubscriptionMessage subMessage = messages[0] as SubscriptionMessage;
            if (subMessage == null)
                return false;

            using (DbConnection connection = GetConnection())
            using (DbTransaction tx = connection.BeginTransaction(isolationLevel))
            {
                if (subMessage.subscriptionType == SubscriptionType.Add)
                    Execute(
                        tx,
                        string.Format(
                        "SET NOCOUNT ON;" +
                        "INSERT INTO {0} ({1}, {2}) " +
                        "(SELECT @{1} AS {1}, @{2} AS {2} WHERE (NOT EXISTS " +
                        "(SELECT {1} FROM {0} AS S2 WHERE ({1} = @{1}) AND ({2} = @{2}))))",
                                      this.table, subscriberParameterName, messageTypeParameterName),
                        msg.ReturnAddress,
                        subMessage.typeName
                        );

                if (subMessage.subscriptionType == SubscriptionType.Remove)
                    Execute(
                        tx,
                        string.Format("SET NOCOUNT ON; DELETE FROM {0} WHERE {1}=@{1} AND {2}=@{2}",
                                      this.table, subscriberParameterName, messageTypeParameterName),
                        msg.ReturnAddress,
                        subMessage.typeName
                        );

                tx.Commit();
            }

            return true;
        }

        public void Init()
        {
            if (this.connectionString == null ||
                this.factory == null ||
                this.messageTypeParameterName == null ||
                this.subscriberParameterName == null ||
                this.table == null)
                throw new ConfigurationErrorsException(
                    "ConnectionString, MessageTypeParameterName, SubscriberParameterName, Table, or ProviderInvariantName have not been set.");
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
    }
}

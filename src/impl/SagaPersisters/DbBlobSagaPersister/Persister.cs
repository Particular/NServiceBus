using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using NServiceBus.Saga;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ObjectBuilder;

namespace DbBlobSagaPersister
{
    public class Persister : ISagaPersister
    {
        private IBuilder builder;
        private DbConnection connection;
        private string onlineTableName;
        private string completedTableName;
        private string idColumnName;
        private string valueColumnName;
        private string connectionString;
        private IsolationLevel isolationLevel = IsolationLevel.ReadCommitted;

        public IsolationLevel IsolationLevel
        {
            set
            {
                this.isolationLevel = value;
            }
        }

        public string ValueColumnName
        {
            set { valueColumnName = value; }
        }

        public string IdColumnName
        {
            set { idColumnName = value; }
        }

        public string CompletedTableName
        {
            set { completedTableName = value; }
        }

        public string OnlineTableName
        {
            set { onlineTableName = value; }
        }

        public IBuilder Builder
        {
            set { builder = value; }
        }

        public string ProviderInvariantName
        {
            set
            {
                DbProviderFactory factory = DbProviderFactories.GetFactory(value);
                this.connection = factory.CreateConnection();

                InitializeConnection();
            }
        }

        private void InitializeConnection()
        {
            if (this.connectionString != null && this.connection != null)
            {
                this.connection.ConnectionString = this.connectionString;
                this.connection.Open();
            }
        }

        public string ConnectionString
        {
            get { return connectionString; }
            set
            {
                connectionString = value;

                InitializeConnection();
            }
        }

        #region ISagaPersister Members

        public void Complete(ISagaEntity saga)
        {
            using (DbTransaction tx = connection.BeginTransaction(isolationLevel))
            {
                DbCommand command = this.connection.CreateCommand();
                command.Transaction = tx;
                command.CommandType = CommandType.Text;

                DbParameter idParam = command.CreateParameter();
                idParam.ParameterName = "@Id";
                idParam.Value = saga.Id;
                command.Parameters.Add(idParam);

                command.CommandText =
                    string.Format("SET NOCOUNT ON; DELETE FROM {0} WHERE {1}=@Id",
                                  this.onlineTableName, this.idColumnName);

                command.ExecuteNonQuery();

                InsertOrUpdate(
                    tx,
                    string.Format("SET NOCOUNT ON; INSERT INTO {0} ({1}, {2}) VALUES (@Id, @Value)",
                                  this.completedTableName, this.idColumnName, this.valueColumnName),
                    saga
                    );

                tx.Commit();
            }
        }

        public ISagaEntity Get(Guid sagaId)
        {
            DbCommand command = this.connection.CreateCommand();
            command.CommandType = CommandType.Text;

            DbParameter idParam = command.CreateParameter();
            idParam.ParameterName = "@Id";
            idParam.Value = sagaId;
            command.Parameters.Add(idParam);

            command.CommandText =
    string.Format("SELECT {0} FROM {1} WHERE {2}=@Id",
        this.valueColumnName, this.onlineTableName, this.idColumnName);

            object result = command.ExecuteScalar();
            byte[] buffer = result as byte[];

            return Deserialize(buffer);
        }

        public void Save(ISagaEntity saga)
        {
            using (DbTransaction tx = connection.BeginTransaction(isolationLevel))
            {
                InsertOrUpdate(
                    tx,
                    string.Format("SET NOCOUNT ON; INSERT INTO {0} ({1}, {2}) VALUES (@Id, @Value)",
                                  this.onlineTableName, this.idColumnName, this.valueColumnName),
                    saga
                    );

                tx.Commit();
            }
        }

        public void Update(ISagaEntity saga)
        {
            using (DbTransaction tx = connection.BeginTransaction(isolationLevel))
            {
                InsertOrUpdate(
                    tx,
                    string.Format("SET NOCOUNT ON; UPDATE {0} SET {1}=@Value WHERE {2}=@Id",
                                  this.onlineTableName, this.valueColumnName, this.idColumnName),
                    saga
                    );

                tx.Commit();
            }
            
        }
 
        #endregion

        #region db helper

       private static void InsertOrUpdate(DbTransaction tx, string sql, ISagaEntity saga)
        {
            DbCommand command = tx.Connection.CreateCommand();
            command.Transaction = tx;
            command.CommandType = CommandType.Text;

            DbParameter idParam = command.CreateParameter();
            idParam.ParameterName = "@Id";
            idParam.Value = saga.Id;
            command.Parameters.Add(idParam);

            DbParameter valueParam = command.CreateParameter();
            valueParam.ParameterName = "@Value";
            valueParam.Value = Serialize(saga);
            command.Parameters.Add(valueParam);

            command.CommandText = sql;

            command.ExecuteNonQuery();
        }

        #endregion

        #region Serialization

        private static byte[] Serialize(ISagaEntity saga)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, saga);

            return stream.ToArray();
        }

        private ISagaEntity Deserialize(byte[] buffer)
        {
            if (buffer == null)
                return null;

            MemoryStream stream = new MemoryStream(buffer);
            BinaryFormatter formatter = new BinaryFormatter();

            object result = formatter.Deserialize(stream);

            object built = this.builder.Build(result.GetType());

            foreach(FieldInfo fi in result.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                if (fi.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length > 0)
                    fi.SetValue(result, fi.GetValue(built));

            return result as ISagaEntity;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            this.connection.Dispose();
        }

        #endregion
    }
}

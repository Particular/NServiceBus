namespace NServiceBus.SQLServer.Transport
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Threading;
    using Serializers.Json;
    using Unicast.Queuing;

    public class SqlServerMessageSender : ISendMessages, IDisposable
    {
        private const string SqlSend =
            @"INSERT INTO [{0}] ([Id],[CorrelationId],[ReplyToAddress],[Recoverable],[Expires],[Headers],[Body]) 
                                    VALUES (@Id,@CorrelationId,@ReplyToAddress,@Recoverable,@Expires,@Headers,@Body)";

        private static readonly JsonMessageSerializer Serializer = new JsonMessageSerializer(null);
        private readonly ThreadLocal<SqlTransaction> currentTransaction = new ThreadLocal<SqlTransaction>();
        private bool disposed;

        public string ConnectionString { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Send(TransportMessage message, Address address)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                string sql = string.Format(SqlSend, address.Queue);
                connection.Open();
                using (var command = new SqlCommand(sql, connection) {CommandType = CommandType.Text})
                {
                    if (currentTransaction.IsValueCreated)
                    {
                        command.Transaction = currentTransaction.Value;
                    }
                    command.Parameters.Add("Id", SqlDbType.UniqueIdentifier).Value = Guid.Parse(message.Id);
                    command.Parameters.Add("CorrelationId", SqlDbType.VarChar).Value = GetValue(message.CorrelationId);
                    if (message.ReplyToAddress == null) // Sendonly endpoint
                    {
                        command.Parameters.Add("ReplyToAddress", SqlDbType.VarChar).Value = DBNull.Value;
                    }
                    else
                    {
                        command.Parameters.Add("ReplyToAddress", SqlDbType.VarChar).Value = message.ReplyToAddress.ToString();
                    }
                    command.Parameters.Add("Recoverable", SqlDbType.Bit).Value = message.Recoverable;
                    if (message.TimeToBeReceived.Ticks > DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks ||
                        message.TimeToBeReceived.Ticks < DateTime.MinValue.Ticks - DateTime.UtcNow.Ticks)
                    {
                        command.Parameters.Add("Expires", SqlDbType.DateTime).Value = DBNull.Value;
                    }
                    else
                    {
                        command.Parameters.Add("Expires", SqlDbType.DateTime).Value = DateTime.UtcNow.Add(message.TimeToBeReceived);
                    }
                    command.Parameters.Add("Headers", SqlDbType.VarChar).Value = Serializer.SerializeObject(message.Headers);
                    if (message.Body == null)
                    {
                        command.Parameters.Add("Body", SqlDbType.VarBinary).Value = DBNull.Value;
                    }
                    else
                    {
                        command.Parameters.Add("Body", SqlDbType.VarBinary).Value = message.Body;
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        public void SetTransaction(SqlTransaction transaction)
        {
            currentTransaction.Value = transaction;
        }

        private static object GetValue(object value)
        {
            return value ?? DBNull.Value;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed resources.
                currentTransaction.Dispose();
            }

            disposed = true;
        }

        ~SqlServerMessageSender()
        {
            Dispose(false);
        }
    }
}
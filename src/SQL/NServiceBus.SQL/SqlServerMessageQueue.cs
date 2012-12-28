namespace NServiceBus.SQL
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Text;
    using System.Threading;
    using Logging;
    using Serialization;
    using Serializers.Binary;
    using Serializers.Json;
    using Unicast.Queuing;

    public class SqlServerMessageQueue : ISendMessages, IReceiveMessages
    {
     
        public string ConnectionString { get; set; }
        public IMessageSerializer MessageSerializer { get; set; }
        public bool PurgeOnStartup { get; set; }
        public int SleepTimeBetweenPolls { get; set; }

        public void Send(TransportMessage message, Address address)
        {
            string body = string.Empty;

            if (MessageSerializer is MessageSerializer)
            {
                body = Convert.ToBase64String(message.Body);
            }
            else if (message.Body != null)
            {
                var stream = new MemoryStream(message.Body) { Position = 0 };
                using (TextReader textReader = new StreamReader(stream))
                {
                    body = textReader.ReadToEnd();
                }
            }

            using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = string.Format(SqlSend, address);
                connection.Open();
                using (var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text })
                {
                    command.Parameters.Add("Id", SqlDbType.UniqueIdentifier).Value = Guid.Parse(message.Id);
                    command.Parameters.Add("CorrelationId", SqlDbType.VarChar).Value = GetValue(message.CorrelationId);
                    if (message.ReplyToAddress == null) // Sendonly endpoint
                        command.Parameters.AddWithValue("ReplyToAddress", string.Empty);
                    else
                        command.Parameters.AddWithValue("ReplyToAddress", message.ReplyToAddress.ToString());
                    command.Parameters.AddWithValue("Recoverable", message.Recoverable);
                    command.Parameters.AddWithValue("MessageIntent", message.MessageIntent.ToString());
                    command.Parameters.Add("TimeToBeReceived", SqlDbType.BigInt).Value = message.TimeToBeReceived.Ticks;
                    command.Parameters.AddWithValue("Headers", Serializer.SerializeObject(message.Headers));
                    command.Parameters.AddWithValue("Body", body);

                    command.ExecuteNonQuery();
                }
            }
        }

        
        public void Init(Address address, bool transactional)
        {
            currentEndpointName = address.ToString();

            if (PurgeOnStartup)
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    var sql = string.Format(SqlPurge, currentEndpointName);
                    connection.Open();
                    using (var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text })
                    {
                        var numberOfPurgedRows = command.ExecuteNonQuery();

                        Logger.InfoFormat("{0} messages was purged from table {1}", numberOfPurgedRows, currentEndpointName);
                    }
                }
            }

        }


        public TransportMessage Receive()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = string.Format(SqlReceive, currentEndpointName);
                connection.Open();
                using (var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text })
                {
                    using (var dataReader = command.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (dataReader.Read())
                        {
                            var id = dataReader.GetGuid(0).ToString();

                            var correlationId = dataReader.IsDBNull(1) ? null : dataReader.GetString(1);
                            var replyToAddress = Address.Parse(dataReader.GetString(2));
                            var recoverable = dataReader.GetBoolean(3);

                            MessageIntentEnum messageIntent;
                            Enum.TryParse(dataReader.GetString(4), out messageIntent);

                            var timeToBeReceived = TimeSpan.FromTicks(dataReader.GetInt64(5));
                            var headers = Serializer.DeserializeObject<Dictionary<string, string>>(dataReader.GetString(6));
                            var tmpBody = dataReader.GetString(7);

                            byte[] body;

                            if (MessageSerializer is MessageSerializer)
                            {
                                body = Convert.FromBase64String(tmpBody);
                            }
                            else
                            {
                                body = Encoding.UTF8.GetBytes(tmpBody);
                            }

                            var message = new TransportMessage
                            {
                                Id = id,
                                CorrelationId = correlationId,
                                ReplyToAddress = replyToAddress,
                                Recoverable = recoverable,
                                MessageIntent = messageIntent,
                                TimeToBeReceived = timeToBeReceived,
                                Headers = headers,
                                Body = body
                            };

                            return message;
                        }
                    }
                }
            }
            if (SleepTimeBetweenPolls > 0)
                Thread.Sleep(SleepTimeBetweenPolls);
            else
                Thread.Sleep(1000);
            return null;
        }

        static object GetValue(object value)
        {
            return value ?? DBNull.Value;
        }


        string currentEndpointName;
        static readonly JsonMessageSerializer Serializer = new JsonMessageSerializer(null);

        const string SqlReceive = @"WITH message AS (SELECT TOP(1) * FROM [{0}] WITH (UPDLOCK, READPAST) ORDER BY TimeStamp ASC) 
			DELETE FROM message 
			OUTPUT deleted.Id, deleted.CorrelationId, deleted.ReplyToAddress, 
			deleted.Recoverable, deleted.MessageIntent, deleted.TimeToBeReceived, deleted.Headers, deleted.Body;";
        
        const string SqlSend = @"INSERT INTO [{0}] ([Id],[CorrelationId],[ReplyToAddress],[Recoverable],[MessageIntent],[TimeToBeReceived],[Headers],[Body]) 
                                    VALUES (@Id,@CorrelationId,@ReplyToAddress,@Recoverable,@MessageIntent,@TimeToBeReceived,@Headers,@Body)";

        const string SqlPurge = @"DELETE FROM [{0}]";

        static readonly ILog Logger = LogManager.GetLogger("Transports.SqlServer");
    }

}

namespace NServiceBus.Unicast.Queuing.SQLServer
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Text;
    using Serializers.Binary;
    using Serialization;
    using Serializers.Json;
    using Transport;
    using Utils;

    public class SqlServerMessageQueue : ISendMessages, IReceiveMessages
    {
        private string currentEndpointName;
        static readonly JsonMessageSerializer Serializer = new JsonMessageSerializer(null);

        private const string SqlReceive = @"WITH message AS (SELECT TOP(1) * FROM [{0}] WITH (UPDLOCK, READPAST) ORDER BY TimeStamp ASC) 
			DELETE FROM message 
			OUTPUT deleted.Id, deleted.IdForCorrelation, deleted.CorrelationId, deleted.ReplyToAddress, 
			deleted.Recoverable, deleted.MessageIntent, deleted.TimeToBeReceived, deleted.Headers, deleted.Body;";
        private const string SqlSend = @"INSERT INTO [{0}] ([Id],[IdForCorrelation],[CorrelationId],[ReplyToAddress],[Recoverable],[MessageIntent],[TimeToBeReceived],[Headers],[Body]) 
                                    VALUES (@Id,@IdForCorrelation,@CorrelationId,@ReplyToAddress,@Recoverable,@MessageIntent,@TimeToBeReceived,@Headers,@Body)";

        public string ConnectionString { get; set; }
        public IMessageSerializer MessageSerializer { get; set; }

        public void Send(TransportMessage message, Address address)
        {
            string body = string.Empty;          
            
            if (MessageSerializer is MessageSerializer)
            {
                body = Convert.ToBase64String(message.Body);
            }
            else if (message.Body != null)
            {
                var stream = new MemoryStream(message.Body) {Position = 0};
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
                    var id = GuidCombGenerator.Generate();

                    command.Parameters.Add("Id", SqlDbType.UniqueIdentifier).Value = id;
                    command.Parameters.Add("IdForCorrelation", SqlDbType.VarChar).Value = GetValue(message.IdForCorrelation);
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

                    message.Id = id.ToString();
                }
            }                           
        }

        private static object GetValue(object value)
        {
            return value ?? DBNull.Value;
        }

        public void Init(Address address, bool transactional)
        {
            currentEndpointName = address.ToString();
        }

        bool IReceiveMessages.HasMessage()
        {
            return true;
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

                            var idForCorrelation = dataReader.IsDBNull(1) ? null : dataReader.GetString(1);
                            var correlationId = dataReader.IsDBNull(2) ? null : dataReader.GetString(2);
                            var replyToAddress = Address.Parse(dataReader.GetString(3));
                            var recoverable = dataReader.GetBoolean(4);

                            MessageIntentEnum messageIntent;
                            Enum.TryParse(dataReader.GetString(5), out messageIntent);

                            var timeToBeReceived = TimeSpan.FromTicks(dataReader.GetInt64(6));
                            var headers = Serializer.DeserializeObject<Dictionary<string, string>>(dataReader.GetString(7));
                            var tmpBody = dataReader.GetString(8);
                            
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
                                IdForCorrelation = idForCorrelation,
                                CorrelationId = correlationId,                                
                                ReplyToAddress = replyToAddress,
                                Recoverable = recoverable,
                                MessageIntent = messageIntent,
                                TimeToBeReceived = timeToBeReceived,                                
                                Headers = headers,
                                Body = body
                            };
                            
                            message.IdForCorrelation = message.GetIdForCorrelation();
                            return message;
                        }
                    }
                }
            }

            return null;
        }
    }
}

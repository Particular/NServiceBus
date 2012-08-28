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

    public class SqlServerMessageQueue : ISendMessages, IReceiveMessages
    {
        private string currentEndpointName;
        static readonly JsonMessageSerializer serializer = new JsonMessageSerializer(null);

        public string ConnectionString { get; set; }
        public IMessageSerializer MessageSerializer { get; set; }  

        #region ISendMessages

        private string SQL_SEND = @" DECLARE @NextId [uniqueidentifier] = NEWID();
                                     
                                     INSERT INTO [{0}] ([Id],[IdForCorrelation],[CorrelationId],[ReplyToAddress],[Recoverable],[MessageIntent],[TimeToBeReceived],[Headers],[Body]) 
                                     VALUES (@NextId,@IdForCorrelation,@CorrelationId,@ReplyToAddress,@Recoverable,@MessageIntent,@TimeToBeReceived,@Headers,@Body);
	                                 
                                     SELECT @NextId";

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
                var sql = string.Format(SQL_SEND, address); 
                connection.Open();                
                using (var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text })
                {
                    command.Parameters.Add("IdForCorrelation", SqlDbType.VarChar).Value = GetValue(message.IdForCorrelation);
                    command.Parameters.Add("CorrelationId", SqlDbType.VarChar).Value = GetValue(message.CorrelationId);
                    if (message.ReplyToAddress == null) // Sendonly endpoint
                        command.Parameters.AddWithValue("ReplyToAddress", string.Empty); 
                    else
                        command.Parameters.AddWithValue("ReplyToAddress", message.ReplyToAddress.ToString()); 
	                command.Parameters.AddWithValue("Recoverable", message.Recoverable); 
	                command.Parameters.AddWithValue("MessageIntent", message.MessageIntent.ToString()); 
                    command.Parameters.Add("TimeToBeReceived", SqlDbType.BigInt).Value = message.TimeToBeReceived.Ticks;
                    command.Parameters.AddWithValue("Headers", serializer.SerializeObject(message.Headers));
                    command.Parameters.AddWithValue("Body", body); 

                    message.Id = command.ExecuteScalar().ToString();
                }
            }                           
        }

        private object GetValue(object value)
        {
            return value ?? DBNull.Value;
        }

        #endregion

        #region IReceiveMessages
        public void Init(Address address, bool transactional)
        {
            currentEndpointName = address.ToString();
        }

        bool IReceiveMessages.HasMessage()
        {
            return true;
        }

        private string SQL_RECEIVE = @" declare @NextId [uniqueidentifier]                                        
                                        declare @IdForCorrelation [varchar](255) 
                                        declare @CorrelationId [varchar](255)                                        
                                        declare @ReplyToAddress [varchar](255) 
                                        declare @Recoverable [bit]
                                        declare @MessageIntent [varchar](16)
                                        declare @TimeToBeReceived [bigint]                                        
                                        declare @Headers [varchar](8000)
                                        declare @Body [varchar](max)

                                        SELECT TOP 1 @NextId=[Id],@IdForCorrelation=IdForCorrelation,@CorrelationId=CorrelationId,@ReplyToAddress=ReplyToAddress,@Recoverable=Recoverable,@MessageIntent=MessageIntent,@TimeToBeReceived=TimeToBeReceived,@Headers=Headers,@Body=Body
                                        FROM [{0}] WITH (UPDLOCK, READPAST)	                                         
                                        ORDER BY TimeStamp ASC;

                                        IF (@NextId IS NOT NULL)
                                        BEGIN		
	                                        SELECT @NextId,@IdForCorrelation,@CorrelationId,@ReplyToAddress,@Recoverable,@MessageIntent,@TimeToBeReceived,@Headers,@Body
	                                        DELETE FROM [{0}] WHERE Id = @NextId
                                        END";
        
        public TransportMessage Receive()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = string.Format(SQL_RECEIVE, currentEndpointName);
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
                            var headers = serializer.DeserializeObject<Dictionary<string, string>>(dataReader.GetString(7));
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
        #endregion        
    }
}

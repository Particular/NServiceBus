using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using NServiceBus.Serializers.Binary;

namespace NServiceBus.Unicast.Queuing.SQLServer
{
    using Serialization;

    public class SqlServerMessageQueue : ISendMessages, IReceiveMessages
    {
        private string currentEndpointName;

        public string ConnectionString { get; set; }
        public IMessageSerializer MessageSerializer { get; set; }  

        #region ISendMessages

        private string SQL_SEND = @"INSERT INTO [{0}] ([IdForCorrelation],[CorrelationId],[ReplyToAddress],[Recoverable],[MessageIntent],[TimeToBeReceived],[TimeSent],[Headers],[Body]) 
                                    VALUES (@IdForCorrelation,@CorrelationId,@ReplyToAddress,@Recoverable,@MessageIntent,@TimeToBeReceived,@TimeSent,@Headers,@Body);
	                                SELECT SCOPE_IDENTITY()";

        public void Send(TransportMessage message, Address address)
        {                                   
            string body;            
            
            if (MessageSerializer is MessageSerializer)
            {
                body = Convert.ToBase64String(message.Body);
            }
            else
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
	                command.Parameters.AddWithValue("IdForCorrelation", message.IdForCorrelation); 
	                command.Parameters.AddWithValue("CorrelationId", message.CorrelationId); 
	                //command.Parameters.AddWithValue("ReturnAddress", message.ReturnAddress); 
	                command.Parameters.AddWithValue("ReplyToAddress", message.ReplyToAddress.ToString()); 
	                command.Parameters.AddWithValue("Recoverable", message.Recoverable); 
	                command.Parameters.AddWithValue("MessageIntent", message.MessageIntent.ToString()); 
	                command.Parameters.AddWithValue("TimeToBeReceived", message.TimeToBeReceived.Ticks); 
	                command.Parameters.AddWithValue("TimeSent", message.TimeSent); 
	                command.Parameters.AddWithValue("Headers", Newtonsoft.Json.JsonConvert.SerializeObject(message.Headers));
                    command.Parameters.AddWithValue("Body", body); 

                    message.Id = command.ExecuteScalar().ToString();
                }
            }                           
        }
        #endregion

        #region IReceiveMessages

        private string DDL =
                @"IF NOT  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{0}]') AND type in (N'U'))
                  BEGIN
                    CREATE TABLE [dbo].[{0}](
	                    [Id] [int] IDENTITY(1,1) NOT NULL,	                    
	                    [IdForCorrelation] [varchar](255) NOT NULL,
	                    [CorrelationId] [varchar](255) NOT NULL,	                    
	                    [ReplyToAddress] [varchar](255) NOT NULL,
	                    [Recoverable] [bit] NOT NULL,
	                    [MessageIntent] [varchar](16) NOT NULL,
	                    [TimeToBeReceived] [bigint] NOT NULL,
	                    [TimeSent] [datetime] NOT NULL,
	                    [Headers] [varchar](8000) NOT NULL,
	                    [Body] [varchar](max) NOT NULL
                    ) ON [PRIMARY]
                  END";

        public void Init(Address address, bool transactional)
        {
            currentEndpointName = address.ToString();

            using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = string.Format(DDL, address);
                connection.Open();
                using (var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text })
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private string SQL_PEEK = @"SELECT TOP 1 Id
	                                FROM [{0}] WITH (UPDLOCK, READPAST)	                                      
	                                ORDER BY Id ASC";
        public bool HasMessage()
        {           
            using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = string.Format(SQL_PEEK, currentEndpointName);
                connection.Open();
                using (var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text })
                {                                                                            
                    var value = command.ExecuteScalar();
                    return value != null && (int)value > 0;
                }
            }        
        }

        private string SQL_RECEIVE = @" declare @NextId [int]
                                        declare @IdForCorrelation [varchar](255) 
                                        declare @CorrelationId [varchar](255)                                        
                                        declare @ReplyToAddress [varchar](255) 
                                        declare @Recoverable [bit]
                                        declare @MessageIntent [varchar](16)
                                        declare @TimeToBeReceived [bigint]
                                        declare @TimeSent [datetime]
                                        declare @Headers [varchar](8000)
                                        declare @Body [varchar](max)

                                        SELECT TOP 1 @NextId=[Id],@IdForCorrelation=IdForCorrelation,@CorrelationId=CorrelationId,@ReplyToAddress=ReplyToAddress,@Recoverable=Recoverable,@MessageIntent=MessageIntent,@TimeToBeReceived=TimeToBeReceived,@TimeSent=TimeSent,@Headers=Headers,@Body=Body
                                        FROM [{0}] WITH (UPDLOCK, READPAST)	                                         
                                        ORDER BY Id ASC;

                                        IF (@NextId IS NOT NULL)
                                        BEGIN		
	                                        SELECT @NextId,@IdForCorrelation,@CorrelationId,@ReplyToAddress,@Recoverable,@MessageIntent,@TimeToBeReceived,@TimeSent,@Headers,@Body
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
                            var id = dataReader.GetInt32(0);
                            var idForCorrelation = dataReader.GetString(1);
                            var correlationId = dataReader.GetString(2);
                            var replyToAddress = Address.Parse(dataReader.GetString(3));
                            var recoverable = dataReader.GetBoolean(4);

                            MessageIntentEnum messageIntent;
                            Enum.TryParse(dataReader.GetString(5), out messageIntent);

                            var timeToBeReceived = TimeSpan.FromTicks(dataReader.GetInt64(6));
                            var timeSent = dataReader.GetDateTime(7);
                            var headers = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(dataReader.GetString(8));
                            var tmpBody = dataReader.GetString(9);
                            
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
                                Id = id.ToString(),
                                IdForCorrelation = idForCorrelation,
                                CorrelationId = correlationId,                                
                                ReplyToAddress = replyToAddress,
                                Recoverable = recoverable,
                                MessageIntent = messageIntent,
                                TimeToBeReceived = timeToBeReceived,
                                TimeSent = timeSent,
                                Headers = headers,
                                Body = body
                            };

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

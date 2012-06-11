using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace NServiceBus.Unicast.Queuing.SQLServer
{
    using Serialization;

    public class SqlServerMessageQueue : ISendMessages, IReceiveMessages
    {
        private string currentEndpointName;

        public string ConnectionString { get; set; }
        public IMessageSerializer MessageSerializer { get; set; }  

        #region ISendMessages
        public void Send(TransportMessage message, Address address)
        {
            var stream = new MemoryStream();
            MessageSerializer.Serialize(new[] { message }, stream);
            
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("exec Send", connection) { CommandType = CommandType.StoredProcedure })
                {                                       
                    command.Parameters.AddWithValue("endpoint", address.ToString());
                    command.Parameters.AddWithValue("envelope", stream.ToArray());

                    command.ExecuteNonQuery();
                }
            }               
        }
        #endregion

        #region IReceiveMessages
        public void Init(Address address, bool transactional)
        {
            currentEndpointName = address.ToString();
        }

        public bool HasMessage()
        {           
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("exec Peek", connection) {CommandType = CommandType.StoredProcedure})
                {                    
                    command.Parameters.AddWithValue("endpoint", currentEndpointName);                                        
                    return command.ExecuteScalar() != null;
                }
            }        
        }

        public TransportMessage Receive()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("exec Receive", connection) { CommandType = CommandType.StoredProcedure })
                {
                    command.Parameters.AddWithValue("endpoint", currentEndpointName);
                    var envelope = command.ExecuteScalar() as Byte[];

                    if (envelope != null)
                    {                        
                        var stream = new MemoryStream(envelope) {Position = 0};
                        return MessageSerializer.Deserialize(stream)[0] as TransportMessage;
                    }
                }
            }

            return null;
        }
        #endregion
    }
}

namespace NServiceBus.Gateway.Persistence.Sql
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    public class SqlPersistence:IPersistMessages
    {
        public string ConnectionString { get; set; }

        public bool InsertMessage(string clientId, DateTime timeReceived, Stream message, IDictionary<string, string> headers)
        {
            int results;

            var stream = new MemoryStream();
            serializer.Serialize(stream, headers);
            stream.Position = 0;
            
            using (stream)
            using (var cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {

                    var cmd = cn.CreateCommand();
                    cmd.CommandText =
                        "IF NOT EXISTS (SELECT Status FROM Messages WHERE (ClientId = @ClientId)) INSERT INTO Messages  (DateTime, ClientId, Status, Message, Headers) VALUES (@DateTime, @ClientId, 0, @Message, @Headers)";

                    cmd.Parameters.AddWithValue("DateTime", timeReceived);
                    cmd.Parameters.AddWithValue("ClientId", clientId);
                    cmd.Parameters.AddWithValue("Message", message);
                    cmd.Parameters.AddWithValue("Headers", stream);
                    results = cmd.ExecuteNonQuery();

                    tx.Commit();
                }
            }

            return results > 0;
        }

        public bool AckMessage(string clientId, out byte[] message, out  IDictionary<string, string> headers)
        {
            message = null;
            headers = null;

            using (var cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    var cmd = cn.CreateCommand();
                    cmd.CommandText =
                        "UPDATE Messages SET Status=1 WHERE (Status=0) AND (ClientId=@ClientId); SELECT Message, Headers FROM Messages WHERE (ClientId = @ClientId) AND (@@ROWCOUNT = 1)";

                    cmd.Parameters.AddWithValue("ClientId", clientId);

                    var ackOk = false;

                    using (var reader = cmd.ExecuteReader())
                    if (reader.Read())
                    {
                        message = (byte[]) reader.GetValue(0);

                        var serHeaders = (byte[]) reader.GetValue(1);
                        var stream = new MemoryStream(serHeaders);
                        var o = serializer.Deserialize(stream);
                        stream.Close();
                        headers = o as IDictionary<string,string>;

                        ackOk = true;
                    }

                    tx.Commit();

                    return ackOk;
                }
            }
        }

        public void UpdateHeader(string clientId, string headerKey, string newValue)
        {
            using (var cn = new SqlConnection(ConnectionString))
            using (var writeStream = new MemoryStream())
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    var read = cn.CreateCommand();
                    read.CommandText = "SELECT Headers FROM Messages WHERE ClientId = @ClientId";
                    read.Parameters.AddWithValue("ClientId", clientId);

                    IDictionary<string, string> o = null;
                    using (var reader = read.ExecuteReader())
                        if (reader.Read())
                            using (var readStream = new MemoryStream((byte[])reader.GetValue(0)))
                                o = serializer.Deserialize(readStream) as IDictionary<string, string>;

                    o[headerKey] = newValue;
                    serializer.Serialize(writeStream, o);
                    writeStream.Position = 0;

                    var write = cn.CreateCommand();
                    write.CommandText = "UPDATE Messages SET Headers=@Headers WHERE ClientId=@ClientId";
                    write.Parameters.AddWithValue("ClientId", clientId);
                    write.Parameters.AddWithValue("Headers", writeStream);
                    var result = write.ExecuteNonQuery();
                    tx.Commit();
                }
            }
        }

        public int DeleteDeliveredMessages(DateTime until)
        {
            int result;

            using (var cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    var cmd = cn.CreateCommand();
                    cmd.CommandText =
                        "DELETE FROM Messages WHERE (DATEDIFF(second, @DateTime, DateTime) < 0) AND (STATUS=1)";

                    cmd.Parameters.AddWithValue("DateTime", until);

                    result = cmd.ExecuteNonQuery();

                    tx.Commit();
                }

                return result;
            }
        }

        private BinaryFormatter serializer = new BinaryFormatter();
    }
}

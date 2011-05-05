﻿namespace NServiceBus.Gateway.Persistence.Sql
{
    using System;
    using System.Collections.Specialized;
    using System.Data.SqlClient;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    public class SqlPersistence:IPersistMessages
    {
        public string ConnectionString { get; set; }

        public bool InsertMessage(string clientId, DateTime timeReceived, byte[] message, NameValueCollection headers)
        {
            int results;

            var stream = new MemoryStream();
            serializer.Serialize(stream, headers);
            
            using (stream)
            using (var cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {

                    var cmd = cn.CreateCommand();
                    cmd.CommandText =
                        "IF NOT EXISTS (SELECT Status FROM Messages WHERE (ClientId = @ClientId)) INSERT INTO Messages  (DateTime, ClientId, Status, Message, Headers) VALUES (@DateTime, @ClientId, 0, @Message, @Headers)";

                    var datetimeIdParam = cmd.CreateParameter();
                    datetimeIdParam.ParameterName = "@DateTime";
                    datetimeIdParam.Value = timeReceived;
                    cmd.Parameters.Add(datetimeIdParam);

                    var clientIdParam = cmd.CreateParameter();
                    clientIdParam.ParameterName = "@ClientId";
                    clientIdParam.Value = clientId;
                    cmd.Parameters.Add(clientIdParam);

                    var messageParam = cmd.CreateParameter();
                    messageParam.ParameterName = "@Message";
                    messageParam.Value = message;
                    cmd.Parameters.Add(messageParam);

                    var headersParam = cmd.CreateParameter();
                    headersParam.ParameterName = "@Headers";
                    headersParam.Value = stream.GetBuffer();
                    cmd.Parameters.Add(headersParam);

                    results = cmd.ExecuteNonQuery();

                    tx.Commit();
                }
            }

            return results > 0;
        }

        public void AckMessage(string clientId, out byte[] message, out NameValueCollection headers)
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

                    var clientIdParam = cmd.CreateParameter();
                    clientIdParam.ParameterName = "@ClientId";
                    clientIdParam.Value = clientId;
                    cmd.Parameters.Add(clientIdParam);

                    var statusParam = cmd.CreateParameter();
                    statusParam.ParameterName = "@Status";
                    statusParam.Value = 0;
                    cmd.Parameters.Add(statusParam);

                    using (var reader = cmd.ExecuteReader())
                    if (reader.Read())
                    {
                        message = (byte[]) reader.GetValue(0);

                        var serHeaders = (byte[]) reader.GetValue(1);
                        var stream = new MemoryStream(serHeaders);
                        var o = serializer.Deserialize(stream);
                        stream.Close();
                        headers = o as NameValueCollection;
                    }

                    tx.Commit();
                }
            }
        }

        public void UpdateHeader(string clientId, string headerKey, string newValue)
        {
            throw new NotImplementedException();
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

                    var dateParam = cmd.CreateParameter();
                    dateParam.ParameterName = "@DateTime";
                    dateParam.Value = until;
                    cmd.Parameters.Add(dateParam);

                    result = cmd.ExecuteNonQuery();

                    tx.Commit();
                }

                return result;
            }
        }

        private BinaryFormatter serializer = new BinaryFormatter();
    }
}

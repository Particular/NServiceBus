namespace NServiceBus.Gateway.Persistence.Sql
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    [ObsoleteEx(Message = "Please use UseNHibernateGatewayPersister() in the NServiceBus.NHibernate assembly instead.",
        RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
    public class SqlPersistence : IPersistMessages
    {
        public string ConnectionString { get; set; }

        public bool InsertMessage(string clientId, DateTime timeReceived, Stream message,
            IDictionary<string, string> headers)
        {
            int results;

            var stream = new MemoryStream();
            serializer.Serialize(stream, headers);

            using (stream)
            {
                using (var cn = new SqlConnection(ConnectionString))
                {
                    cn.Open();
                    using (var tx = cn.BeginTransaction())
                    {
                        var command = cn.CreateCommand();
                        command.CommandText =
                            "IF NOT EXISTS (SELECT Status FROM Messages WHERE (ClientId = @ClientId)) INSERT INTO Messages  (DateTime, ClientId, Status, Message, Headers) VALUES (@DateTime, @ClientId, 0, @Message, @Headers)";

                        var datetimeIdParam = command.CreateParameter();
                        datetimeIdParam.ParameterName = "@DateTime";
                        datetimeIdParam.Value = timeReceived;
                        command.Parameters.Add(datetimeIdParam);

                        var clientIdParam = command.CreateParameter();
                        clientIdParam.ParameterName = "@ClientId";
                        clientIdParam.Value = clientId;
                        command.Parameters.Add(clientIdParam);

                        var messageParam = command.CreateParameter();
                        messageParam.ParameterName = "@Message";
                        var ms = message as MemoryStream;
                        if (ms == null)
                        {
                            messageParam.Value = message;
                        }
                        else
                        {
                            messageParam.Value = ms.ToArray();
                        }
                        command.Parameters.Add(messageParam);

                        var headersParam = command.CreateParameter();
                        headersParam.ParameterName = "@Headers";
                        headersParam.Value = stream.ToArray();
                        command.Parameters.Add(headersParam);

                        results = command.ExecuteNonQuery();

                        tx.Commit();
                    }
                }
            }

            return results > 0;
        }

        public bool AckMessage(string clientId, out byte[] message, out IDictionary<string, string> headers)
        {
            message = null;
            headers = null;

            using (var cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    var command = cn.CreateCommand();
                    command.CommandText =
                        "UPDATE Messages SET Status=1 WHERE (Status=0) AND (ClientId=@ClientId); SELECT Message, Headers FROM Messages WHERE (ClientId = @ClientId) AND (@@ROWCOUNT = 1)";

                    var clientIdParam = command.CreateParameter();
                    clientIdParam.ParameterName = "@ClientId";
                    clientIdParam.Value = clientId;
                    command.Parameters.Add(clientIdParam);

                    var statusParam = command.CreateParameter();
                    statusParam.ParameterName = "@Status";
                    statusParam.Value = 0;
                    command.Parameters.Add(statusParam);

                    var ackOk = false;

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            message = (byte[]) reader.GetValue(0);

                            var serializedHeaders = (byte[]) reader.GetValue(1);
                            var stream = new MemoryStream(serializedHeaders);
                            var o = serializer.Deserialize(stream);
                            stream.Close();
                            headers = o as IDictionary<string, string>;

                            ackOk = true;
                        }
                    }

                    tx.Commit();

                    return ackOk;
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
                    var command = cn.CreateCommand();
                    command.CommandText =
                        "DELETE FROM Messages WHERE (DATEDIFF(second, @DateTime, DateTime) < 0) AND (STATUS=1)";

                    var dateParam = command.CreateParameter();
                    dateParam.ParameterName = "@DateTime";
                    dateParam.Value = until;
                    command.Parameters.Add(dateParam);

                    result = command.ExecuteNonQuery();

                    tx.Commit();
                }

                return result;
            }
        }

        readonly BinaryFormatter serializer = new BinaryFormatter();
    }
}
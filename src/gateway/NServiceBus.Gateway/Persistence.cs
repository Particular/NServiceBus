using System;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Transactions;

namespace NServiceBus.Gateway
{
    public class Tester
    {
        public void Loop()
        {
            for(int i=0; i < 100; i++)
                TestPersistence();
        }

        public void TestPersistence()
        {
            var p = new Persistence
                        {
                            ConnectionString =
                                @"Data Source=UDIDAHANMOBILE2\SQLEXPRESS;Initial Catalog=model;Integrated Security=True"
                        };

            var clientId = Guid.NewGuid().ToString();
            var md5 = new byte[16] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6};
            var msg = new byte[] {8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8};
            var headers = new NameValueCollection();
            headers.Add("hello", "world");

            var sw = new Stopwatch();
            sw.Start();
            using (var scope = new TransactionScope())
            {
                p.InsertMessage(DateTime.UtcNow, clientId, md5, msg, headers);
                scope.Complete();
            }
            
            Trace.WriteLine("insert:" + sw.ElapsedTicks);
            sw.Reset();

            NameValueCollection outHeaders;
            byte[] outMessage;
            
            
            sw.Start();
            using (var scope = new TransactionScope())
            {
                p.AckMessage(clientId, md5, out outMessage, out outHeaders);
                scope.Complete();
            }
            Trace.WriteLine("ack:" + sw.ElapsedTicks);
            sw.Reset();
            
            sw.Start();
            int deleted;
            using (var scope = new TransactionScope())
            {
                deleted = p.DeleteDeliveredMessages(DateTime.UtcNow - TimeSpan.FromSeconds(2));
                scope.Complete();
            }
            sw.Stop();
            Trace.WriteLine("delete:" + sw.ElapsedTicks);

            if (deleted > 0)
                Trace.WriteLine("DELETED ROWS:" + deleted);
        }
    }

    public class Persistence
    {
        public string ConnectionString { get; set; }

        public bool InsertMessage(DateTime dateTime, string clientId, byte[] md5, byte[] message, NameValueCollection headers)
        {
            int results;

            if (md5.Length != 16)
                throw new ArgumentException("md5 must be 16 bytes.");

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
                        "IF NOT EXISTS (SELECT Status FROM Messages WHERE (ClientId = @ClientId) AND (MD5 = @MD5)) INSERT INTO Messages  (DateTime, ClientId, MD5, Status, Message, Headers) VALUES (@DateTime, @ClientId, @MD5, 0, @Message, @Headers)";

                    var datetimeIdParam = cmd.CreateParameter();
                    datetimeIdParam.ParameterName = "@DateTime";
                    datetimeIdParam.Value = dateTime;
                    cmd.Parameters.Add(datetimeIdParam);

                    var clientIdParam = cmd.CreateParameter();
                    clientIdParam.ParameterName = "@ClientId";
                    clientIdParam.Value = clientId;
                    cmd.Parameters.Add(clientIdParam);

                    var md5Param = cmd.CreateParameter();
                    md5Param.ParameterName = "@MD5";
                    md5Param.Value = md5;
                    cmd.Parameters.Add(md5Param);

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

        public void AckMessage(string clientId, byte[] md5, out byte[] message, out NameValueCollection headers)
        {
            if (md5.Length != 16)
                throw new ArgumentException("md5 must be 16 bytes.");

            message = null;
            headers = null;

            using (var cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    var cmd = cn.CreateCommand();
                    cmd.CommandText =
                        "UPDATE Messages SET Status=1 WHERE (Status=0) AND (ClientId=@ClientId) AND (MD5=@MD5); SELECT Message, Headers FROM Messages WHERE (ClientId = @ClientId) AND (MD5 = @MD5) AND (@@ROWCOUNT = 1)";

                    var clientIdParam = cmd.CreateParameter();
                    clientIdParam.ParameterName = "@ClientId";
                    clientIdParam.Value = clientId;
                    cmd.Parameters.Add(clientIdParam);

                    var md5Param = cmd.CreateParameter();
                    md5Param.ParameterName = "@MD5";
                    md5Param.Value = md5;
                    cmd.Parameters.Add(md5Param);

                    var statusParam = cmd.CreateParameter();
                    statusParam.ParameterName = "@Status";
                    statusParam.Value = 0;
                    cmd.Parameters.Add(statusParam);

                    var reader = cmd.ExecuteReader();
                    if (!reader.Read())
                        return;

                    message = (byte[]) reader.GetValue(0);

                    var serHeaders = (byte[]) reader.GetValue(1);
                    var stream = new MemoryStream(serHeaders);
                    var o = serializer.Deserialize(stream);
                    stream.Close();
                    headers = o as NameValueCollection;

                    reader.Close();

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

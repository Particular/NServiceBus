using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace NServiceBus.Gateway
{
    public class Tester
    {
        public void Loop()
        {
            for(int i=0; i < 10000; i++)
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

            var sw = new Stopwatch();
            sw.Start();
                p.InsertMessage(DateTime.UtcNow, clientId, md5, msg, "headers");
            sw.Stop();
            Trace.WriteLine("insert:" + sw.ElapsedTicks);
            
            sw.Restart();
                var message = p.AckMessage(clientId, md5);
            sw.Stop();
            Trace.WriteLine("ack:" + sw.ElapsedTicks);

            sw.Restart();
            var deleted = p.DeleteDeliveredMessages(DateTime.UtcNow - TimeSpan.FromSeconds(2));
            sw.Stop();
            Trace.WriteLine("delete:" + sw.ElapsedTicks);

            if (deleted > 0)
                Trace.WriteLine("DELETED ROWS:" + deleted);
        }
    }

    public class Persistence
    {
        public string ConnectionString { get; set; }

        public bool InsertMessage(DateTime dateTime, string clientId, byte[] md5, byte[] message, string headers)
        {
            int results;

            if (md5.Length != 16)
                throw new ArgumentException("md5 must be 16 bytes.");

            using (var cn = new SqlConnection(ConnectionString))
            {
                cn.Open();

                var cmd = cn.CreateCommand();
                cmd.CommandText = "IF NOT EXISTS (SELECT Status FROM Messages WHERE (ClientId = @ClientId) AND (MD5 = @MD5)) INSERT INTO Messages  (DateTime, ClientId, MD5, Status, Message, Headers) VALUES (@DateTime, @ClientId, @MD5, 0, @Message, @Headers)";

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
                headersParam.Value = message;
                cmd.Parameters.Add(headersParam);

                results = cmd.ExecuteNonQuery();
            }

            return results > 0;
        }

        public byte[] AckMessage(string clientId, byte[] md5)
        {
            byte[] message;

            if (md5.Length != 16)
                throw new ArgumentException("md5 must be 16 bytes.");
            
            using (var cn = new SqlConnection(ConnectionString))
            {
                cn.Open();

                var cmd = cn.CreateCommand();
                cmd.CommandText = "UPDATE Messages SET Status=1 WHERE (Status=0) AND (ClientId=@ClientId) AND (MD5=@MD5); SELECT Message FROM Messages WHERE (ClientId = @ClientId) AND (MD5 = @MD5) AND (@@ROWCOUNT = 1)";

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

                message = (byte[])cmd.ExecuteScalar();
            }

            return message;
        }

        public int DeleteDeliveredMessages(DateTime until)
        {
            int result;

            using (var cn = new SqlConnection(ConnectionString))
            {
                cn.Open();

                var cmd = cn.CreateCommand();
                cmd.CommandText = "DELETE FROM Messages WHERE (DATEDIFF(second, @DateTime, DateTime) < 0) AND (STATUS=1)";

                var dateParam = cmd.CreateParameter();
                dateParam.ParameterName = "@DateTime";
                dateParam.Value = until;
                cmd.Parameters.Add(dateParam);

                result = cmd.ExecuteNonQuery();
            }

            return result;
        }
    }
}

namespace NServiceBus.Gateway.Deduplication
{
    using System;
    using System.Data.SqlClient;

    [ObsoleteEx(Message = "Please use UseNHibernateGatewayDeduplication() in the NServiceBus.NHibernate assembly instead.", RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
    public class SqlDeduplication : IDeduplicateMessages
    {
        public string ConnectionString { get; set; }

        public bool DeduplicateMessage(string clientId, DateTime timeReceived)
        {
            using (var cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {

                    var cmd = cn.CreateCommand();
                    cmd.CommandText =
                        "IF NOT EXISTS (SELECT 1 FROM MessageDeduplication WHERE ClientId = @ClientId) INSERT INTO MessageDeduplication (DateTime, ClientId) VALUES (@DateTime, @ClientId)";

                    var datetimeIdParam = cmd.CreateParameter();
                    datetimeIdParam.ParameterName = "@DateTime";
                    datetimeIdParam.Value = timeReceived;
                    cmd.Parameters.Add(datetimeIdParam);

                    var clientIdParam = cmd.CreateParameter();
                    clientIdParam.ParameterName = "@ClientId";
                    clientIdParam.Value = clientId;
                    cmd.Parameters.Add(clientIdParam);

                    var results = cmd.ExecuteNonQuery();
                    tx.Commit();
                    return results > 0;
                }
            }
        }

        public int DeleteDeliveredMessages(DateTime until)
        {
            using (var cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    var cmd = cn.CreateCommand();
                    cmd.CommandText =
                        "DELETE FROM MessageDeduplication WHERE DATEDIFF(second, @DateTime, DateTime) < 0";

                    var dateParam = cmd.CreateParameter();
                    dateParam.ParameterName = "@DateTime";
                    dateParam.Value = until;
                    cmd.Parameters.Add(dateParam);

                    var result = cmd.ExecuteNonQuery();
                    tx.Commit();
                    return result;
                }
            }
        }
    }
}

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Transports.Msmq;

    class SqlServerSubscriptionReader : IQuerySubscriptions
    {
        string subscriptionSchema;
        string subscriptionsTable;
        string connectionString;

        public SqlServerSubscriptionReader(string subscriptionSchema, string subscriptionsTable, string connectionString)
        {
            this.subscriptionSchema = subscriptionSchema;
            this.subscriptionsTable = subscriptionsTable;
            this.connectionString = connectionString;
        }

        public async Task<IEnumerable<Subscriber>> GetSubscribersFor(IEnumerable<Type> eventTypes)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                var results = new List<Subscriber>();
                var typeParams = eventTypes.Select((t, i) => new { TypeName = t.FullName, ParamName = $"@Type{i}" }).ToArray();
                var typeParamsDeclaration = string.Join(", ", typeParams.Select(p => p.ParamName));
                using (var cmd = new SqlCommand($@"SELECT Endpoint, TransportAddress FROM [{subscriptionSchema}].[{subscriptionsTable}] WHERE TypeName in ({typeParamsDeclaration}) GROUP BY Endpoint, TransportAddress", conn))
                {
                    foreach (var typeParam in typeParams)
                    {
                        cmd.Parameters.Add(typeParam.ParamName, SqlDbType.VarChar).Value = typeParam.TypeName;
                    }
                    using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (reader.Read())
                        {
                            var endpoint = reader.GetString(0);
                            var transportAddress = reader.GetString(1);
                            var subscription = new Subscriber(endpoint, transportAddress);
                            results.Add(subscription);
                        }
                    }
                }
                return results;
            }
        }
    }
}
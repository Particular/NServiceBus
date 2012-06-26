using System.Data;
using System.Data.SqlClient;

namespace NServiceBus.Unicast.Queuing.SQLServer
{
    public class SqlServerQueueCreator : ICreateQueues
    {
        private const string Ddl =
                @"IF NOT  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{0}]') AND type in (N'U'))
                  BEGIN
                    CREATE TABLE [dbo].[{0}](
	                    [Id] [int] IDENTITY(1,1) NOT NULL,	                    
	                    [IdForCorrelation] [varchar](255),
	                    [CorrelationId] [varchar](255),
	                    [ReplyToAddress] [varchar](255) NOT NULL,
	                    [Recoverable] [bit] NOT NULL,
	                    [MessageIntent] [varchar](16) NOT NULL,
	                    [TimeToBeReceived] [bigint],	                    
	                    [Headers] [varchar](8000) NOT NULL,
	                    [Body] [varchar](max) NOT NULL
                    ) ON [PRIMARY]
                  END";

        public void CreateQueueIfNecessary(Address address, string account)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = string.Format(Ddl, address);
                connection.Open();

                using (var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text })
                {
                    command.ExecuteNonQuery();
                }
            }            
        }

        public string ConnectionString { get; set; }
    }
}
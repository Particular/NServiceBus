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
	                    [Id] [uniqueidentifier] NOT NULL,
	                    [IdForCorrelation] [varchar](255) NULL,
	                    [CorrelationId] [varchar](255) NULL,
	                    [ReplyToAddress] [varchar](255) NOT NULL,
	                    [Recoverable] [bit] NOT NULL,
	                    [MessageIntent] [varchar](16) NOT NULL,
	                    [TimeToBeReceived] [bigint] NULL,
	                    [Headers] [varchar](8000) NOT NULL,
	                    [Body] [varchar](max) NOT NULL,
	                    [TimeStamp] [timestamp]
                    ) ON [PRIMARY];                    

                    CREATE CLUSTERED INDEX [Index_TimeStamp] ON [dbo].[{0}] 
                    (
	                    [TimeStamp] ASC
                    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
                    
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
namespace NServiceBus.Gateway.Tests.Idempotency
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Transactions;
    using Persistence;
    using Persistence.Sql;

    public class Persistenstester
    {
        public void Loop()
        {
            for(int i=0; i < 100; i++)
                TestPersistence();
        }

        public void TestPersistence()
        {
            var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            var p = new SqlPersistence
                        {
                            ConnectionString = connectionString
                        };

            var clientId = Guid.NewGuid().ToString();
            var msg = new byte[] {8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8};
            var headers = new Dictionary<string,string> {{"hello", "world"}};

            var sw = new Stopwatch();
            sw.Start();
            using (var scope = new TransactionScope())
            using(var msgStream  = new MemoryStream(msg))
            {
                p.InsertMessage(clientId,DateTime.UtcNow, msgStream, headers);
                scope.Complete();
            }
            
            Trace.WriteLine("insert:" + sw.ElapsedTicks);
            sw.Reset();

            IDictionary<string,string> outHeaders;
            byte[] outMessage;
            
            
            sw.Start();
            using (var scope = new TransactionScope())
            {
                p.AckMessage(clientId, out outMessage, out outHeaders);
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
}
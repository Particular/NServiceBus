using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace NServiceBus.Timeout.Hosting.Azure
{
    public class ServiceContext : TableServiceContext
    {
        public ServiceContext(string baseAddress, StorageCredentials credentials)
            : base(baseAddress, credentials)
        {
        }

        public const string TimeoutDataEntityTableName = "TimeoutData";

        public IQueryable<TimeoutDataEntity> TimeoutData
        {
            get
            {
                return this.CreateQuery<TimeoutDataEntity>(TimeoutDataEntityTableName);
            }
        }
    }
}
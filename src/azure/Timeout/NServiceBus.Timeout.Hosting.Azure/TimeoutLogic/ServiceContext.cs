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

        public static string TimeoutManagerDataTableName = "TimeoutManagerData";

        public IQueryable<TimeoutManagerDataEntity> TimeoutManagerData
        {
            get
            {
                return this.CreateQuery<TimeoutManagerDataEntity>(TimeoutManagerDataTableName);
            }
        }

        public static string TimeoutDataTableName = "TimeoutData";

        public IQueryable<TimeoutDataEntity> TimeoutData
        {
            get
            {
                return this.CreateQuery<TimeoutDataEntity>(TimeoutDataTableName);
            }
        }

    }
}
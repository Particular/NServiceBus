namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using Routing;

    class InstanceMappingTable : FeatureStartupTask
    {
        public InstanceMappingTable(string filePath, TimeSpan checkInterval, IAsyncTimer timer, IInstanceMappingFileAccess fileAccess, EndpointInstances endpointInstances)
        {
            this.filePath = filePath;
            this.checkInterval = checkInterval;
            this.timer = timer;
            this.fileAccess = fileAccess;
            this.endpointInstances = endpointInstances;
        }

        protected override Task OnStart(IMessageSession session)
        {
            timer.Start(() =>
            {
                ReloadData();
                return TaskEx.CompletedTask;
            }, checkInterval, ex => log.Error("Unable to update instance mapping information because the instance mapping file couldn't be read.", ex));
            return TaskEx.CompletedTask;
        }

        public void ReloadData()
        {
            try
            {
                var doc = fileAccess.Load(filePath);
                var instances = parser.Parse(doc);
                endpointInstances.AddOrReplaceInstances("FileRoutingTable", instances.ToList());
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while reading the endpoint instance mapping file at {filePath}. See the inner exception for more details.", ex);
            }
        }

        protected override Task OnStop(IMessageSession session) => timer.Stop();

        TimeSpan checkInterval;
        IInstanceMappingFileAccess fileAccess;
        string filePath;
        EndpointInstances endpointInstances;
        InstanceMappingFileParser parser = new InstanceMappingFileParser();
        IAsyncTimer timer;

        static readonly ILog log = LogManager.GetLogger(typeof(InstanceMappingTable));
    }
}
namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using Routing;

    class FileRoutingTable : FeatureStartupTask
    {
        public FileRoutingTable(string filePath, TimeSpan checkInterval, IAsyncTimer timer, IRoutingFileAccess fileAccess)
        {
            this.filePath = filePath;
            this.checkInterval = checkInterval;
            this.timer = timer;
            this.fileAccess = fileAccess;
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

                var newInstanceMap = instances
                .GroupBy(i => i.Endpoint)
                .ToDictionary(g => g.Key, g => new HashSet<EndpointInstance>(g));

                instanceMap = newInstanceMap;
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while reading the endpoint instance mapping file at {filePath}. See the inner exception for more details.", ex);
            }
        }

        public Task<IEnumerable<EndpointInstance>> FindInstances(string endpoint)
        {
            HashSet<EndpointInstance> result;

            // ReSharper disable once PossibleNullReferenceException
            return instanceMap.TryGetValue(endpoint, out result)
                ? Task.FromResult<IEnumerable<EndpointInstance>>(result)
                : noInstances;
        }

        protected override Task OnStop(IMessageSession session) => timer.Stop();

        TimeSpan checkInterval;
        IRoutingFileAccess fileAccess;
        string filePath;

        Dictionary<string, HashSet<EndpointInstance>> instanceMap;
        FileRoutingTableParser parser = new FileRoutingTableParser();
        IAsyncTimer timer;
        Task<IEnumerable<EndpointInstance>> noInstances = Task.FromResult(Enumerable.Empty<EndpointInstance>());

        static readonly ILog log = LogManager.GetLogger(typeof(FileRoutingTable));
    }
}
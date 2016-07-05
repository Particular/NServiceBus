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
        //TODO: remove maxLoadAttempts?
        public FileRoutingTable(string filePath, TimeSpan checkInterval, IAsyncTimer timer, IRoutingFileAccess fileAccess, int maxLoadAttempts)
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
            }, checkInterval, ex => log.Error($"An error occurred while reading the endpoint instance mapping file at {filePath}. See the inner exception for more details.", ex));
            return TaskEx.CompletedTask;
        }

        public void ReloadData()
        {
            var doc = fileAccess.Load(filePath);
            var instances = parser.Parse(doc);

            var newInstanceMap = instances
                .GroupBy(i => i.Endpoint)
                .ToDictionary(g => g.Key, g => new HashSet<EndpointInstance>(g));

            instanceMap = newInstanceMap;
        }

        public Task<IEnumerable<EndpointInstance>> FindInstances(string endpoint)
        {
            HashSet<EndpointInstance> result;

            // ReSharper disable once PossibleNullReferenceException
            return instanceMap.TryGetValue(endpoint, out result)
                ? Task.FromResult<IEnumerable<EndpointInstance>>(result)
                : Task.FromResult(noInstances);
        }

        protected override Task OnStop(IMessageSession session) => timer.Stop();

        TimeSpan checkInterval;
        IRoutingFileAccess fileAccess;
        string filePath;

        Dictionary<string, HashSet<EndpointInstance>> instanceMap;
        FileRoutingTableParser parser = new FileRoutingTableParser();
        IAsyncTimer timer;
        IEnumerable<EndpointInstance> noInstances = new EndpointInstance[0];

        static readonly ILog log = LogManager.GetLogger(typeof(FileRoutingTable));
    }
}
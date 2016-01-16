namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Routing;
    using NServiceBus.Settings;

    class FileRoutingTable : FeatureStartupTask
    {
        public FileRoutingTable(string filePath, TimeSpan checkInterval, IAsyncTimer timer, IRoutingFileAccess fileAccess, int maxLoadAttempts, ReadOnlySettings settings)
        {
            this.settings = settings;
            this.filePath = filePath;
            this.checkInterval = checkInterval;
            this.timer = timer;
            this.fileAccess = fileAccess;
            this.maxLoadAttempts = maxLoadAttempts;
        }

        protected override async Task OnStart(IBusSession context)
        {
            var endpointInstances = settings.Get<EndpointInstances>();
            endpointInstances.AddDynamic(FindInstances);

            await ReloadData().ConfigureAwait(false);

            timer.Start(ReloadData, checkInterval, ex => log.Error("Error while reading routing table", ex));
        }

        async Task ReloadData()
        {
            var doc = await ReadFileWithRetries().ConfigureAwait(false);
            var instances = parser.Parse(doc);

            var newInstanceMap = instances
                .GroupBy(i => i.Endpoint)
                .ToDictionary(g => g.Key, g => Task.FromResult((IEnumerable<EndpointInstance>) new HashSet<EndpointInstance>(g)));

            instanceMap = newInstanceMap;
        }

        async Task<XDocument> ReadFileWithRetries()
        {
            var attempt = 0;
            while (true)
            {
                try
                {
                    var result = fileAccess.Load(filePath);
                    return result;
                }
                catch (Exception ex)
                {
                    attempt++;
                    if (attempt < maxLoadAttempts)
                    {
                        if (log.IsDebugEnabled)
                        {
                            log.Debug("Error while reading routing file", ex);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }

        Task<IEnumerable<EndpointInstance>> FindInstances(EndpointName endpoint)
        {
            Task<IEnumerable<EndpointInstance>> result;
            return instanceMap.TryGetValue(endpoint, out result) ? result : emptyEndpointInstancesTask;
        }

        protected override Task OnStop(IBusSession context)
        {
            return timer.Stop();
        }

        TimeSpan checkInterval;
        IRoutingFileAccess fileAccess;
        string filePath;

        Dictionary<EndpointName, Task<IEnumerable<EndpointInstance>>> instanceMap = new Dictionary<EndpointName, Task<IEnumerable<EndpointInstance>>>();
        int maxLoadAttempts;
        FileRoutingTableParser parser = new FileRoutingTableParser();
        ReadOnlySettings settings;
        IAsyncTimer timer;

        static readonly ILog log = LogManager.GetLogger(typeof(FileRoutingTable));
        static Task<IEnumerable<EndpointInstance>> emptyEndpointInstancesTask = Task.FromResult(Enumerable.Empty<EndpointInstance>());
    }
}
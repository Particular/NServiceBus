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
        static readonly ILog log = LogManager.GetLogger(typeof(FileRoutingTable));

        Dictionary<EndpointName, HashSet<EndpointInstance>> instanceMap = new Dictionary<EndpointName, HashSet<EndpointInstance>>();
        ReadOnlySettings settings;
        string filePath;
        TimeSpan checkInterval;
        IAsyncTimer timer;
        IRoutingFileAccess fileAccess;
        FileRoutingTableParser parser = new FileRoutingTableParser();
        int maxLoadAttempts;
        

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
            var newInstanceMap = new Dictionary<EndpointName, HashSet<EndpointInstance>>();

            foreach (var i in instances)
            {
                HashSet<EndpointInstance> instancesOfThisEndpoint;
                if (!newInstanceMap.TryGetValue(i.Endpoint, out instancesOfThisEndpoint))
                {
                    instancesOfThisEndpoint = new HashSet<EndpointInstance>();
                    newInstanceMap[i.Endpoint] = instancesOfThisEndpoint;
                }
                instancesOfThisEndpoint.Add(i);
            }
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

        IEnumerable<EndpointInstance> FindInstances(EndpointName endpoint)
        {
            HashSet<EndpointInstance> result;
            if (instanceMap.TryGetValue(endpoint, out result))
            {
                return result;
            }
            return Enumerable.Empty<EndpointInstance>();
        }

        protected override Task OnStop(IBusSession context)
        {
            return timer.Stop();
        }
    }
}
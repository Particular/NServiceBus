namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Features;
    using Logging;
    using Routing;
    using Settings;

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

            errorMessage = $"An error occured while reading the endpoint instance mapping file at {filePath}. See the inner exception for more details.";
        }

        protected override async Task OnStart(IMessageSession session)
        {
            var endpointInstances = settings.Get<EndpointInstances>();
            endpointInstances.AddDynamic(FindInstances);

            try
            {
                await ReloadData().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }

            timer.Start(ReloadData, checkInterval, ex => log.Error(errorMessage, ex));
        }

        async Task ReloadData()
        {
            var doc = await ReadFileWithRetries().ConfigureAwait(false);
            var instances = parser.Parse(doc);

            var newInstanceMap = instances
                .GroupBy(i => i.Endpoint)
                .ToDictionary(g => g.Key, g => Task.FromResult((IEnumerable<EndpointInstance>)new HashSet<EndpointInstance>(g)));

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
                            log.Debug(errorMessage, ex);
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

        Task<IEnumerable<EndpointInstance>> FindInstances(string endpoint)
        {
            Task<IEnumerable<EndpointInstance>> result;
            return instanceMap.TryGetValue(endpoint, out result) ? result : emptyEndpointInstancesTask;
        }

        protected override Task OnStop(IMessageSession session)
        {
            return timer.Stop();
        }

        TimeSpan checkInterval;
        IRoutingFileAccess fileAccess;
        string filePath;
        string errorMessage;

        Dictionary<string, Task<IEnumerable<EndpointInstance>>> instanceMap = new Dictionary<string, Task<IEnumerable<EndpointInstance>>>();
        int maxLoadAttempts;
        FileRoutingTableParser parser = new FileRoutingTableParser();
        ReadOnlySettings settings;
        IAsyncTimer timer;

        static readonly ILog log = LogManager.GetLogger(typeof(FileRoutingTable));
        static Task<IEnumerable<EndpointInstance>> emptyEndpointInstancesTask = Task.FromResult(Enumerable.Empty<EndpointInstance>());
    }
}
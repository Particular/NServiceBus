namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Features;
    using Logging;
    using Routing;

    class FileRoutingTable : FeatureStartupTask
    {
        public FileRoutingTable(string filePath, TimeSpan checkInterval, IAsyncTimer timer, IRoutingFileAccess fileAccess, int maxLoadAttempts)
        {
            this.filePath = filePath;
            this.checkInterval = checkInterval;
            this.timer = timer;
            this.fileAccess = fileAccess;
            this.maxLoadAttempts = maxLoadAttempts;

            errorMessage = $"An error occurred while reading the endpoint instance mapping file at {filePath}. See the inner exception for more details.";
        }

        protected override Task OnStart(IMessageSession session)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception($"The endpoint instance mapping file {filePath} does not exist.");
            }

            return TaskEx.CompletedTask;
        }

        async Task ReloadData()
        {
            var doc = await ReadFileWithRetries().ConfigureAwait(false);
            var instances = parser.Parse(doc);

            var newInstanceMap = instances
                .GroupBy(i => i.Endpoint)
                .ToDictionary(g => g.Key, g => new HashSet<EndpointInstance>(g));

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

        public async Task<IEnumerable<EndpointInstance>> FindInstances(string endpoint)
        {
            HashSet<EndpointInstance> result;
            if (instanceMap == null)
            {
                try
                {
                    await ReloadData().ConfigureAwait(false);
                    timer.Start(ReloadData, checkInterval, ex => log.Error(errorMessage, ex));
                }
                catch (Exception ex)
                {
                    throw new Exception(errorMessage, ex);
                }
            }
            // ReSharper disable once PossibleNullReferenceException
            return instanceMap.TryGetValue(endpoint, out result)
                ? result
                : Enumerable.Empty<EndpointInstance>();
        }

        protected override Task OnStop(IMessageSession session) => timer.Stop();

        TimeSpan checkInterval;
        IRoutingFileAccess fileAccess;
        string filePath;
        string errorMessage;

        Dictionary<string, HashSet<EndpointInstance>> instanceMap;
        int maxLoadAttempts;
        FileRoutingTableParser parser = new FileRoutingTableParser();
        IAsyncTimer timer;

        static readonly ILog log = LogManager.GetLogger(typeof(FileRoutingTable));
    }
}
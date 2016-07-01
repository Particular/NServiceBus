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

            if (!Path.IsPathRooted(filePath))
            {
                this.filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            }

            errorMessage = $"An error occurred while reading the endpoint instance mapping file at {filePath}. See the inner exception for more details.";
        }

        protected override async Task OnStart(IMessageSession session)
        {
            if (File.Exists(filePath))
            {
                var doc = await ReadFileWithRetries().ConfigureAwait(false);
                parser.Parse(doc, true);
            }
        }

        async Task ReloadData()
        {
            var doc = await ReadFileWithRetries().ConfigureAwait(false);
            var instances = parser.Parse(doc, false);

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
                : new HashSet<EndpointInstance>()
                {
                    new EndpointInstance(endpoint)
                };
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
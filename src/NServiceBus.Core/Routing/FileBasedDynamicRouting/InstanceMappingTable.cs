namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using Routing;

    class InstanceMappingTable : FeatureStartupTask
    {
        public InstanceMappingTable(string filePath, TimeSpan checkInterval, IAsyncTimer timer, IInstanceMappingFileAccess fileAccess)
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

                LogChanges(instanceMap, newInstanceMap);

                instanceMap = newInstanceMap;
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while reading the endpoint instance mapping file at {filePath}. See the inner exception for more details.", ex);
            }
        }

        void LogChanges(Dictionary<string, HashSet<EndpointInstance>> oldInstanceMap, Dictionary<string, HashSet<EndpointInstance>> newInstanceMap)
        {
            var output = new StringBuilder();
            var hasChanges = false;
            output.AppendLine($"Updating instance mapping table from '{filePath}':");

            foreach (var endpoint in newInstanceMap)
            {
                HashSet<EndpointInstance> existingInstances;
                if (oldInstanceMap.TryGetValue(endpoint.Key, out existingInstances))
                {
                    var newInstances = endpoint.Value.Except(existingInstances).Count();
                    var removedInstances = existingInstances.Except(endpoint.Value).Count();

                    if (newInstances > 0 || removedInstances > 0)
                    {
                        output.AppendLine($"Updated endpoint '{endpoint.Key}': +{Instances(newInstances)}, -{Instances(removedInstances)}");
                        hasChanges = true;
                    }
                }
                else
                {
                    output.AppendLine($"Added endpoint '{endpoint.Key}' with {Instances(endpoint.Value.Count)}");
                    hasChanges = true;
                }
            }

            foreach (var removedEndpoint in oldInstanceMap.Keys.Except(newInstanceMap.Keys))
            {
                output.AppendLine($"Removed all instances of endpoint '{removedEndpoint}'");
                hasChanges = true;
            }

            if (hasChanges)
            {
                log.Info(output.ToString());
            }
        }

        static string Instances(int count)
        {
            return count > 1 ? $"{count} instances" : $"{count} instance";
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
        IInstanceMappingFileAccess fileAccess;
        string filePath;

        Dictionary<string, HashSet<EndpointInstance>> instanceMap = new Dictionary<string, HashSet<EndpointInstance>>(0);
        InstanceMappingFileParser parser = new InstanceMappingFileParser();
        IAsyncTimer timer;
        Task<IEnumerable<EndpointInstance>> noInstances = Task.FromResult(Enumerable.Empty<EndpointInstance>());

        static readonly ILog log = LogManager.GetLogger(typeof(InstanceMappingTable));
    }
}
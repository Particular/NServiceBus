namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;

    /// <summary>
    /// </summary>
    public class HostConfiguration
    {
        /// <summary>
        /// </summary>
        /// <param name="hostId"></param>
        /// <param name="diagnosticsBasePath"></param>
        /// <param name="onCriticalErrorAction"></param>
        public HostConfiguration(string hostId,string diagnosticsBasePath, Func<ICriticalErrorContext, Task> onCriticalErrorAction)
        {
            this.hostId = hostId;
            this.diagnosticsBasePath = diagnosticsBasePath;
            this.onCriticalErrorAction = onCriticalErrorAction;
        }

        internal StartableHost Build(EndpointConfiguration endpointConfiguration)
        {
            //guid sucks here, we should use a string instead in the future
            if (Guid.TryParse(hostId, out var guidHostId))
            {
                guidHostId = DeterministicGuid.Create(hostId);
            }

            var hostInfo = endpointConfiguration.UniquelyIdentifyRunningInstance();

            hostInfo.UsingCustomIdentifier(guidHostId);

            endpointConfiguration.DefineCriticalErrorAction(async context =>
            {
                //Update status and stuff

                //do what the user wants
                await onCriticalErrorAction(context).ConfigureAwait(false);

                //other actions
            });


            //the host should use extension points as much as possible to enrich messages etc. Eg. the Audit and Error enrichers should be move here eventually
            endpointConfiguration.Pipeline.Register(b => new AddOriginatingHostIdHeaders(hostId), "Add hostid on outgoing messages");

            endpoints.Add(endpointConfiguration);
         
            return new StartableHost(endpointConfiguration.Build());
        }
        
        string hostId;
        string diagnosticsBasePath;//logging, dumps of config settings , etc would go here. We would also expose this to downstreams so they can write stuff in here
        Func<ICriticalErrorContext, Task> onCriticalErrorAction;
        List<EndpointConfiguration> endpoints = new List<EndpointConfiguration>();
    }

    class AddOriginatingHostIdHeaders : Behavior<IOutgoingPhysicalMessageContext>
    {
        string hostId;

        public AddOriginatingHostIdHeaders(string hostId)
        {
            this.hostId = hostId;
        }

        public override Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
        {
            context.Headers["OriginatingHost"] = hostId;

            return next();
        }
    }

    /// <summary>
    /// </summary>
    public static class Host
    {
        /// <summary>
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="endpointConfiguration"></param>
        /// <returns></returns>
        public static async Task<HostInstance> Start(HostConfiguration configuration, EndpointConfiguration endpointConfiguration)
        {
            var startable = configuration.Build(endpointConfiguration);


            return await startable.Start().ConfigureAwait(false);
        }
    }

    class StartableHost
    {
        public StartableHost(InitializableEndpoint endpoint)
        {
            this.endpoint = endpoint;
        }

        public async Task<HostInstance> Start()
        {
            var startable = await endpoint.Initialize().ConfigureAwait(false);
            var instance = await startable.Start().ConfigureAwait(false);

            return new HostInstance(instance);
        }

        InitializableEndpoint endpoint;
    }

    /// <summary>
    /// </summary>
    public class HostInstance
    {
        internal HostInstance(IEndpointInstance endpointInstance)
        {
            this.endpointInstance = endpointInstance;
        }

        /// <summary>
        /// 
        /// </summary>
        public IEndpointInstance EndpointInstance => endpointInstance;

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public Task Stop()
        {
            return endpointInstance.Stop();
        }

        IEndpointInstance endpointInstance;
    }
}
using System;
using Common.Logging;
using NServiceBus.ObjectBuilder;
using System.Linq;

namespace NServiceBus.Host
{
    public class GenericHost : MarshalByRefObject
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(GenericHost));

        private IMessageEndpoint messageEndpoint;
        private IStartableBus bus;
        private readonly Type endpointType;
        private readonly IMessageEndpointConfiguration messageEndpointConfiguration;

        public GenericHost(Type endpointType)
        {
            this.endpointType = endpointType;

            messageEndpointConfiguration = GetEndpointConfiguration();
        }

        public void Start()
        {
            logger.Debug("Starting host for " + endpointType.Name);
            var busConfiguration = messageEndpointConfiguration.ConfigureBus();

            //register the endpoint so that the user can get DI for the endpoint itself
            busConfiguration.Configurer.ConfigureComponent(endpointType, ComponentCallModelEnum.Singleton);

            bus = busConfiguration.CreateBus();

            //build the endpoint
            messageEndpoint = busConfiguration.Builder.Build<IMessageEndpoint>();

            bus.Start();

            messageEndpoint.OnStart();
        }

        public void Stop()
        {
            messageEndpoint.OnStop();

            bus.Dispose();
        }

        private IMessageEndpointConfiguration GetEndpointConfiguration()
        {
            var endpointConfigurationType = endpointType.Assembly.GetTypes()
                .Where(t => typeof(IMessageEndpointConfiguration).IsAssignableFrom(t)).FirstOrDefault();

            if (endpointConfigurationType == null)
                throw new InvalidOperationException("No endpoint configuration found in assembly " +
                                                    endpointType.Assembly);
            return Activator.CreateInstance(endpointConfigurationType) as IMessageEndpointConfiguration;
        }
    }
}
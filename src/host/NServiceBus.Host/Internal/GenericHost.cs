using System;
using System.Diagnostics;
using Common.Logging;
using System.Collections.Specialized;

namespace NServiceBus.Host.Internal
{
    /// <summary>
    /// Implementation which hooks into TopShelf's Start/Stop lifecycle.
    /// </summary>
    public class GenericHost : MarshalByRefObject
    {
        /// <summary>
        /// Does startup work.
        /// </summary>
        public void Start()
        {
            Trace.WriteLine("Starting host for " + endpointType.FullName);

            var configurationSpecifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);
          

            var busConfiguration = new ConfigurationBuilder(configurationSpecifier).Build();
          
            Action startupAction = null;

            if (configurationSpecifier is ISpecify.StartupAction)
                startupAction = (configurationSpecifier as ISpecify.StartupAction).StartupAction;

            messageEndpoint = Configure.ObjectBuilder.Build<IMessageEndpoint>();

            if (!(configurationSpecifier is IDontWant.TheBusStartedAutomatically))
                busConfiguration.CreateBus().Start(startupAction);

            if (messageEndpoint == null)
                return;

            //give it its own thread so that logging continues to work.
            Action onstart = () => messageEndpoint.OnStart();
            onstart.BeginInvoke(null, null);
        }

        /// <summary>
        /// Does shutdown work.
        /// </summary>
        public void Stop()
        {
            if (messageEndpoint != null)
                messageEndpoint.OnStop();
        }

        /// <summary>
        /// Accepts the type which will specify the users custom configuration.
        /// This type should implement <see cref="IConfigureThisEndpoint"/>.
        /// </summary>
        /// <param name="endpointType"></param>
        public GenericHost(Type endpointType)
        {
            this.endpointType = endpointType;
        }

        private readonly Type endpointType;
        private IMessageEndpoint messageEndpoint;
    }
}
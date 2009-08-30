using System;
using Common.Logging;

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
            var commandLine = string.Join(" ", args).ToLowerInvariant();

            var profileConfigurationHandlers = profileManager.GetProfileConfigurationHandlersFor(commandLine);
 
            var busConfiguration  = new ConfigurationBuilder(specifier, profileConfigurationHandlers).Build();
          
            Action startupAction = null;

            if (specifier is ISpecify.StartupAction)
                startupAction = (specifier as ISpecify.StartupAction).StartupAction;

            messageEndpoint = Configure.ObjectBuilder.Build<IMessageEndpoint>();

            if (!(specifier is IDontWant.TheBusStartedAutomatically))
                busConfiguration.CreateBus().Start(startupAction);

            if (messageEndpoint == null)
                return;

            //give it its own thread so that logging continues to work.
            Action onstart = () =>
                                 {
                                     var logger = LogManager.GetLogger(messageEndpoint.GetType());
                                     try
                                     {
                                         logger.Debug("Starting the message endpoint.");
                                         messageEndpoint.OnStart();
                                     }
                                     catch (Exception ex)
                                     {
                                         logger.Error("Problem occurred when starting the endpoint.", ex);

                                         //don't rethrow so that thread doesn't die before log message is shown.
                                     }
                                     
                                 };

            onstart.BeginInvoke(null, null);
        }

        /// <summary>
        /// Does shutdown work.
        /// </summary>
        public void Stop()
        {
            if (messageEndpoint != null)
            {
                var logger = LogManager.GetLogger(messageEndpoint.GetType());
                logger.Debug("Stopping endpoint.");
                try
                {
                    messageEndpoint.OnStop();
                }
                catch (Exception ex)
                {
                    logger.Error("Could not stop endpoint.", ex);
                    
                    // no need to rethrow, closing the process anyway
                }
            }
        }

        /// <summary>
        /// Accepts the type which will specify the users custom configuration.
        /// This type should implement <see cref="IConfigureThisEndpoint"/>.
        /// </summary>
        /// <param name="endpointType"></param>
        /// <param name="args"></param>
        public GenericHost(Type endpointType, string[] args)
        {
            this.args = args;
           
            specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);

            var assembliesToScan = new[] {GetType().Assembly,specifier.GetType().Assembly};

            profileManager = new ProfileManager(assembliesToScan,specifier);
        }

        private IMessageEndpoint messageEndpoint;
        private readonly IConfigureThisEndpoint specifier;
        private readonly string[] args;
        private readonly ProfileManager profileManager;

    }
}
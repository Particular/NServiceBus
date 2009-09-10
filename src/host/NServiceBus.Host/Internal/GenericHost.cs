using System;
using System.Collections.Generic;
using Common.Logging;

namespace NServiceBus.Host.Internal
{
    /// <summary>
    /// Implementation which hooks into TopShelf's Start/Stop lifecycle.
    /// </summary>
    public class GenericHost : MarshalByRefObject
    {
        /// <summary>
        /// Event raised when configuration is complete
        /// </summary>
        public static event EventHandler ConfigurationComplete;

        /// <summary>
        /// Does startup work.
        /// </summary>
        public void Start()
        {
            var profileConfigurationHandlers = profileManager.GetProfileConfigurationHandlersFor(args);
 
            var busConfiguration  = new ConfigurationBuilder(specifier, profileConfigurationHandlers).Build();
          
            Action startupAction = null;

            if (specifier is ISpecify.StartupAction)
                startupAction = (specifier as ISpecify.StartupAction).StartupAction;

            if (ConfigurationComplete != null)
                ConfigurationComplete(this, null);

            thingsToRunAtStartup = Configure.ObjectBuilder.BuildAll<IWantToRunAtStartup>();

            if (!(specifier is IDontWant.TheBusStartedAutomatically))
                busConfiguration.CreateBus().Start(startupAction);

            if (thingsToRunAtStartup == null)
                return;

            foreach (var thing in thingsToRunAtStartup)
            {
                var toRun = thing;
                Action onstart = () =>
                                     {
                                         var logger = LogManager.GetLogger(toRun.GetType());
                                         try
                                         {
                                             logger.Debug("Calling " + toRun.GetType().Name);
                                             toRun.Run();
                                         }
                                         catch (Exception ex)
                                         {
                                             logger.Error("Problem occurred when starting the endpoint.", ex);

                                             //don't rethrow so that thread doesn't die before log message is shown.
                                         }

                                     };

                onstart.BeginInvoke(null, null);
            }
        }

        /// <summary>
        /// Does shutdown work.
        /// </summary>
        public void Stop()
        {
            if (thingsToRunAtStartup == null)
                return;

            foreach (var thing in thingsToRunAtStartup)
            {
                if (thing != null)
                {
                    var logger = LogManager.GetLogger(thing.GetType());
                    logger.Debug("Stopping endpoint.");
                    try
                    {
                        thing.Stop();
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Could not stop endpoint.", ex);

                        // no need to rethrow, closing the process anyway
                    }
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

        private IEnumerable<IWantToRunAtStartup> thingsToRunAtStartup;
        private readonly IConfigureThisEndpoint specifier;
        private readonly string[] args;
        private readonly ProfileManager profileManager;

    }
}
using System;
using System.Diagnostics;
using Common.Logging;
using System.Collections.Specialized;
using System.Linq;

namespace NServiceBus.Host.Internal
{
    /// <summary>
    /// Implementation which hooks into TopShelf's Start/Stop lifecycle.
    /// </summary>
    public class GenericHost : MarshalByRefObject
    {
        public static ModeEnum Mode { get; private set; }

        /// <summary>
        /// Does startup work.
        /// </summary>
        public void Start()
        {
            Trace.WriteLine("Starting host for " + endpointType.FullName);

            var configurationSpecifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);

            var busConfiguration = new ConfigurationBuilder(configurationSpecifier, modeConfig).Build();
          
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
        /// <param name="args"></param>
        public GenericHost(Type endpointType, string[] args)
        {
            this.endpointType = endpointType;

            var a = string.Join(" ", args);

            if (a.Contains(Enum.GetName(typeof(ModeEnum), ModeEnum.Integration).ToLower()))
                mode = ModeEnum.Integration;
            else
            {
                if (a.Contains(Enum.GetName(typeof(ModeEnum), ModeEnum.Lite).ToLower()))
                    mode = ModeEnum.Lite;
            }

            Mode = mode;

            switch(mode)
            {
                case ModeEnum.Lite:
                    modeConfig = new ConfigureLite();
                    break;
                case ModeEnum.Integration:
                    modeConfig = new ConfigureIntegration();
                    break;
                case ModeEnum.Production:
                    modeConfig = new ConfigureProduction();
                    break;
            }
        }

        private readonly Type endpointType;
        private IMessageEndpoint messageEndpoint;
        private ModeEnum mode = ModeEnum.Production;
        private readonly IModeConfiguration modeConfig = new ConfigureProduction();
    }

    public enum ModeEnum
    {
        Production,
        Integration,
        Lite
    }
}
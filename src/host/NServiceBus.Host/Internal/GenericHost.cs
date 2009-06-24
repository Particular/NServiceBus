using System;
using System.Collections.Generic;
using System.Reflection;
using Common.Logging;
using NServiceBus.ObjectBuilder.Common;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Host.Internal
{
    public class GenericHost : MarshalByRefObject
    {
        public void Start()
        {
            Logger.Debug("Starting host for " + endpointType.Name);

            var specifier = (IConfigureThisEndpoint)Activator.CreateInstance(endpointType);
            Configure cfg;

            if (specifier is ISpecify.TypesToScan)
                cfg = Configure.With((specifier as ISpecify.TypesToScan).TypesToScan);
            else
                if (specifier is ISpecify.AssembliesToScan)
                    cfg = Configure.With(new List<Assembly>((specifier as ISpecify.AssembliesToScan).AssembliesToScan).ToArray());
                else
                    if (specifier is ISpecify.ProbeDirectory)
                        cfg = Configure.With((specifier as ISpecify.ProbeDirectory).ProbeDirectory);
                    else
                        cfg = Configure.With();

            Action startupAction = null;

            if (specifier is ISpecify.StartupAction)
                startupAction = (specifier as ISpecify.StartupAction).StartupAction;

            Type containerType = null;
            Type messageEndpointType = null;
            bool startBusAutomatically = true;

            foreach (var t in endpointType.GetInterfaces())
            {
                var args = t.GetGenericArguments();
                if (args.Length == 1)
                {
                    if (typeof(IContainer).IsAssignableFrom(args[0]))
                        if (typeof(ISpecify.ToUseContainer<>).MakeGenericType(args[0]).IsAssignableFrom(endpointType))
                            containerType = args[0];

                    if (typeof(IMessageEndpoint).IsAssignableFrom(args[0]))
                        if (typeof(ISpecify.ToRun<>).MakeGenericType(args[0]).IsAssignableFrom(endpointType))
                            messageEndpointType = args[0];
                }

                if (t == typeof(IDontWantTheBusStartedAutomatically))
                    startBusAutomatically = false;
            }

            if (containerType != null)
                ObjectBuilder.Common.Config.ConfigureCommon.With(
                                cfg,
                                Activator.CreateInstance(containerType) as IContainer
                                );
            else
                cfg.SpringBuilder();

            if (messageEndpointType != null)
                Configure.TypeConfigurer.ConfigureComponent(messageEndpointType, ComponentCallModelEnum.Singleton);

            specifier.Init(cfg);

            messageEndpoint = Configure.ObjectBuilder.Build<IMessageEndpoint>();

            if (startBusAutomatically)
                cfg.CreateBus().Start(startupAction);

            if (messageEndpoint != null)
                messageEndpoint.OnStart();
        }

        public void Stop()
        {
            if (messageEndpoint != null)
                messageEndpoint.OnStop();
        }

        public GenericHost(Type endpointType)
        {
            this.endpointType = endpointType;
        }

        private readonly Type endpointType;
        private IMessageEndpoint messageEndpoint;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(GenericHost));
    }
}
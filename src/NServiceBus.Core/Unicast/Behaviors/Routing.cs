namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Pipeline;

    class Routing : Feature
    {
        public Routing()
        {
            Func<Address, string> translator = a => a.Queue;
            Defaults(s => s.SetDefault("FileBasedRouting.Translate", translator));
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register<RoutingRegistration>();
            context.Container.RegisterSingleton(new FileBasedRoundRobinRouting(context.Settings.Get<string>("FileBasedRouting.BasePath")));
            context.Container.ConfigureComponent<RoutingBehavior>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(b => b.Translator, context.Settings.Get("FileBasedRouting.Translate"));
        }

        public class RoutingRegistration : RegisterStep
        {
            public RoutingRegistration()
                : base("Routing", typeof(RoutingBehavior), "Changes destinations address to round robin on workers")
            {
                InsertAfter(WellKnownStep.MutateOutgoingTransportMessage);
                InsertAfterIfExists("LogOutgoingMessage");
                InsertBefore(WellKnownStep.DispatchMessageToTransport);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ConfigureRouting
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="basePath"></param>
        /// <param name="translate"></param>
        public static void FileBasedRouting(this BusConfiguration config, string basePath, Func<Address, string> translate)
        {
            config.EnableFeature<Routing>();
            config.Settings.Set("FileBasedRouting.BasePath", basePath);
            config.Settings.Set("FileBasedRouting.Translate", translate);
        }
    }
}
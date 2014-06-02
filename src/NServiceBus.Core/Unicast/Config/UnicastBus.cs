namespace NServiceBus.Features
{
    using System.Linq;
    using Pipeline;
    using Pipeline.Contexts;

    /// <summary>
    ///   Used to configure the <see cref="Unicast.UnicastBus"/>
    /// </summary>
    public class UnicastBus : Feature
    {

        internal UnicastBus()
        {
            EnableByDefault();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<Unicast.UnicastBus>(DependencyLifecycle.SingleInstance);

            ConfigureSubscriptionAuthorization(context);
            RegisterMessageModules(context);

            context.Container.ConfigureComponent<PipelineExecutor>(DependencyLifecycle.SingleInstance);
            ConfigureBehaviors(context);
        }

        void ConfigureBehaviors(FeatureConfigurationContext context)
        {
            // ReSharper disable HeapView.SlowDelegateCreation
            context.Settings.GetAvailableTypes().Where(t => (typeof(IBehavior<IncomingContext>).IsAssignableFrom(t)  || typeof(IBehavior<OutgoingContext>).IsAssignableFrom(t))
                                                            && !(t.IsAbstract || t.IsInterface))
                // ReSharper restore HeapView.SlowDelegateCreation
                .ToList()
                .ForEach(behaviorType => context.Container.ConfigureComponent(behaviorType,DependencyLifecycle.InstancePerCall));
        }


        void RegisterMessageModules(FeatureConfigurationContext context)
        {
            // ReSharper disable HeapView.SlowDelegateCreation
            context.Settings.GetAvailableTypes().Where(t => typeof(IMessageModule).IsAssignableFrom(t)
                                                            && !(t.IsAbstract || t.IsInterface))
                // ReSharper restore HeapView.SlowDelegateCreation
                .ToList()
                .ForEach(moduleType => context.Container.ConfigureComponent(moduleType, DependencyLifecycle.InstancePerCall));
        }

        void ConfigureSubscriptionAuthorization(FeatureConfigurationContext context)
        {
            var authType = context.Settings.GetAvailableTypes().FirstOrDefault(t => typeof(IAuthorizeSubscriptions).IsAssignableFrom(t) && !t.IsInterface);

            if (authType != null)
            {
                context.Container.ConfigureComponent(authType, DependencyLifecycle.SingleInstance);
            }
        }
    }
}
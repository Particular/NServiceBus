namespace NServiceBus.Startup
{
    using System;
    using Features;
    using Pipeline;
    using Pipeline.Contexts;

    internal class InstantiateFeaturesBehavior : BaseConfigurationBehavior, IBehavior<ConfigurationContext>
    {
        public void Invoke(ConfigurationContext context, Action next)
        {
            this.context = context;

            ActivateAndInvoke<Feature>(feature => context.Configure.Features.Add(feature));

            next();
        }
    }
}
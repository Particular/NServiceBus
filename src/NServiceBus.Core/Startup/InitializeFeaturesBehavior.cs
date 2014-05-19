namespace NServiceBus.Startup
{
    using System;
    using Features;
    using Pipeline;
    using Pipeline.Contexts;

    internal class InitializeFeaturesBehavior : IBehavior<ConfigurationContext>
    {
        public void Invoke(ConfigurationContext context, Action next)
        {
            new FeatureInitializer().Run(context.Configure);

            next();
        }
    }
}
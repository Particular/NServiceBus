namespace NServiceBus.Startup
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    internal class INeedInitializationBehavior : BaseConfigurationBehavior, IBehavior<ConfigurationContext>
    {
        public void Invoke(ConfigurationContext context, Action next)
        {
            this.context = context;

            ActivateAndInvoke<INeedInitialization>(t => t.Init(context.Configure));

            next();
        }
    }
}
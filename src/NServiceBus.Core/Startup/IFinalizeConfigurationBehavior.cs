namespace NServiceBus.Startup
{
    using System;
    using Config;
    using Pipeline;
    using Pipeline.Contexts;

    internal class IFinalizeConfigurationBehavior : BaseConfigurationBehavior, IBehavior<ConfigurationContext>
    {
        public void Invoke(ConfigurationContext context, Action next)
        {
            this.context = context;

            ActivateAndInvoke<IFinalizeConfiguration>(t => t.FinalizeConfiguration(context.Configure));
            
            next();
        }
    }
}
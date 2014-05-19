namespace NServiceBus.Startup
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    internal class IWantToRunBeforeConfigurationIsFinalizedBehavior : BaseConfigurationBehavior, IBehavior<ConfigurationContext>
    {
        public void Invoke(ConfigurationContext context, Action next)
        {
            this.context = context;

            ActivateAndInvoke<IWantToRunBeforeConfigurationIsFinalized>(t => t.Run(context.Configure));

            next();
        }
    }
}
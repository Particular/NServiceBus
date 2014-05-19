namespace NServiceBus.Startup
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    internal class IWantToRunBeforeConfigurationBehavior : BaseConfigurationBehavior, IBehavior<ConfigurationContext>
    {
        public void Invoke(ConfigurationContext context, Action next)
        {
            this.context = context;

            ActivateAndInvoke<IWantToRunBeforeConfiguration>(t => t.Init(context.Configure));

            next();
        }
    }
}

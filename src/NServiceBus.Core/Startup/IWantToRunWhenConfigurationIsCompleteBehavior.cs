namespace NServiceBus.Startup
{
    using System;
    using System.Linq;
    using Config;
    using Pipeline;
    using Pipeline.Contexts;

    internal class IWantToRunWhenConfigurationIsCompleteBehavior : IBehavior<ConfigurationContext>
    {
        public void Invoke(ConfigurationContext context, Action next)
        {
            context.Builder.BuildAll<IWantToRunWhenConfigurationIsComplete>()
                .ToList()
                .ForEach(o => o.Run(context.Configure));
        }
    }
}
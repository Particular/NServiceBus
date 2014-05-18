namespace NServiceBus.Startup
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    internal class PreventChangesBehavior : IBehavior<ConfigurationContext>
    {
        public void Invoke(ConfigurationContext context, Action next)
        {
            context.Configure.Settings.PreventChanges();
        }
    }
}
namespace NServiceBus.Startup
{
    using System;
    using System.Linq;
    using Pipeline.Contexts;

    internal class BaseConfigurationBehavior
    {
        protected ConfigurationContext context;

        protected void ActivateAndInvoke<T>(Action<T> action) where T : class
        {
            ForAllTypes<T>(t =>
            {

                var instanceToInvoke = (T) Activator.CreateInstance(t);
                action(instanceToInvoke);
            });

        }

        protected void ForAllTypes<T>(Action<Type> action) where T : class
        {
            context.TypesToScan.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
                .ToList().ForEach(action);
        }
    }
}
#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using global::Spring.Context.Support;

    [Obsolete("Replace with Use configuration.UseContainer<NServiceBus.Spring>(), where configuration is an instance of type BusConfiguration", true)]
    public static class ConfigureSpringBuilder
    {
        [Obsolete("Use configuration.UseContainer<NServiceBus.Spring>(), where configuration is an instance of type BusConfiguration", true)]
        public static Configure SpringFrameworkBuilder(this Configure config)
        {
            throw new NotImplementedException();
        }

        [CLSCompliant(false)]
        [Obsolete("Use configuration.UseContainer<NServiceBus.Spring>(b => b.ExistingApplicationContext(applicationContext)), where configuration is an instance of type BusConfiguration", true)]
        public static Configure SpringFrameworkBuilder(this Configure config, GenericApplicationContext applicationContext)
        {
            throw new NotImplementedException();
        }

    }
}

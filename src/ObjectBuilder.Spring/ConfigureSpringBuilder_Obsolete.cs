#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using global::Spring.Context.Support;

    [Obsolete("Replace with Configure.With(c=>.UseContainer<NServiceBus.Spring>())", true)]
    public static class ConfigureSpringBuilder
    {
        [Obsolete("Replace with Configure.With(c=>.UseContainer<NServiceBus.Spring>())", true)]
        public static Configure SpringFrameworkBuilder(this Configure config)
        {
            throw new NotImplementedException();
        }

        [CLSCompliant(false)]
        [Obsolete("Replace with Configure.With(c => c.UseContainer<NServiceBus.Spring>(b => b.ExistingApplicationContext(applicationContext)));", true)]
        public static Configure SpringFrameworkBuilder(this Configure config, GenericApplicationContext applicationContext)
        {
            throw new NotImplementedException();
        }

    }
}

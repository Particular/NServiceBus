namespace NServiceBus
{
    using System;
    using global::Spring.Context.Support;

    /// <summary>
    /// Contains extension methods to <see cref="Configure"/> for configuring the Spring container.
    /// </summary>
    public static class ConfigureSpringBuilder
    {
        /// <summary>
        /// Use the Spring Framework as the container.
        /// </summary>
        [Obsolete("Replace with Configure.With(c=>.UseContainer<NServiceBus.Spring>())", true)]
// ReSharper disable UnusedParameter.Global
        public static Configure SpringFrameworkBuilder(this Configure config)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Use the Spring Framework as the container with the initialized application context
        /// </summary>
        [CLSCompliant(false)]
        [Obsolete("Replace with Configure.With(c => c.UseContainer<NServiceBus.Spring>(b => b.ExistingApplicationContext(applicationContext)));", true)]
// ReSharper disable UnusedParameter.Global
        public static Configure SpringFrameworkBuilder(this Configure config, GenericApplicationContext applicationContext)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }

    }
}

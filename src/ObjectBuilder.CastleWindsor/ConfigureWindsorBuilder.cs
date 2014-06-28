namespace NServiceBus
{
    using System;
    using Castle.Windsor;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureWindsorBuilder
    {
        /// <summary>
        /// Use the Castle Windsor builder.
        /// </summary>
        [Obsolete("Replace with Configure.With(c=>.UseContainer<Windsor>())", true)]
// ReSharper disable UnusedParameter.Global
        public static Configure CastleWindsorBuilder(this Configure config)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Use the Castle Windsor builder passing in a pre-configured container to be used by nServiceBus.
        /// </summary>
        [Obsolete("Replace with Configure.With(c => c.UseContainer<Windsor>(b => b.ExistingContainer(container)))", true)]
// ReSharper disable UnusedParameter.Global
        public static Configure CastleWindsorBuilder(this Configure config, IWindsorContainer container)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }
    }
}
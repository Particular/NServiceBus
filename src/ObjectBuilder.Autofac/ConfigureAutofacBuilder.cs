namespace NServiceBus
{
    using System;
    using global::Autofac;

    /// <summary>
    /// Contains extension methods to <see cref="Configure"/>.
    /// </summary>
    public static class ConfigureAutofacBuilder
    {
        /// <summary>
        /// Use the Autofac builder.
        /// </summary>
        /// <param name="config">The configuration context.</param>
        /// <returns>The configuration context.</returns>
        [Obsolete("Replace with Configure.With(c=>.UseContainer<AutofacObjectBuilder>())", true)]
// ReSharper disable once UnusedParameter.Global
        public static Configure AutofacBuilder(this Configure config)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Use the Autofac builder passing in a pre-configured container to be used by nServiceBus.
        /// </summary>
        /// <param name="config">The configuration context.</param>
        /// <param name="rootScope">The root-most lifetime scope.</param>
        /// <returns>The configuration context.</returns>
        [Obsolete("Replace with Configure.With(c=>.UseContainer(new AutofacObjectBuilder()))", true)]
// ReSharper disable UnusedParameter.Global
        public static Configure AutofacBuilder(this Configure config, ILifetimeScope rootScope)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }
    }
}
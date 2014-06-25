namespace NServiceBus
{
    using System;
    using global::Ninject;

    /// <summary>
    /// The static class which holds <see cref="NServiceBus"/> extensions methods.
    /// </summary>
    public static class ConfigureNinjectBuilder
    {
        /// <summary>
        /// Instructs <see cref="NServiceBus"/> to use the provided kernel
        /// </summary>
        /// <param name="config">The extended Configure.</param>
        /// <returns>The Configure.</returns>
        [Obsolete("Replace with Configure.With(c=>.UseContainer<NServiceBus.Ninject>())", true)]
// ReSharper disable UnusedParameter.Global
        public static Configure NinjectBuilder(this Configure config)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instructs <see cref="NServiceBus"/> to use the provided kernel
        /// </summary>
        /// <param name="config">The extended Configure.</param>
        /// <param name="kernel">The kernel.</param>
        /// <returns>The Configure.</returns>
        [Obsolete("Replace with Configure.With(c => c.UseContainer<NServiceBus.Ninject>(b => b.ExistingKernel(kernel)))", true)]
// ReSharper disable UnusedParameter.Global
        public static Configure NinjectBuilder(this Configure config, IKernel kernel)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }
    }
}
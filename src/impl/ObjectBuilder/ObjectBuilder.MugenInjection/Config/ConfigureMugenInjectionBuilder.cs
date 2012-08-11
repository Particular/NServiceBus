using MugenInjection.Core;
using MugenInjection.Interface;
using NServiceBus.ObjectBuilder.Common.Config;
using NServiceBus.ObjectBuilder.MugenInjection;

namespace NServiceBus
{
    /// <summary>
    /// The static class which holds <see cref="NServiceBus"/> extensions methods.
    /// </summary>
    public static class ConfigureMugenInjectionBuilder
    {
        /// <summary>
        /// Instructs <see cref="NServiceBus"/> to use the provided injector
        /// </summary>
        /// <param name="config">The extended Configure.</param>
        /// <returns>The Configure.</returns>
        public static Configure MugenInjectionBuilder(this Configure config)
        {
            ConfigureCommon.With(config, new MugenInjectionObjectBuilder());
            return config;
        }

        /// <summary>
        /// Instructs <see cref="NServiceBus"/> to use the provided injector
        /// </summary>
        /// <param name="config">The extended Configure.</param>
        /// <param name="injector">The injector.</param>
        /// <returns>The Configure.</returns>
        public static Configure MugenInjectionBuilder(this Configure config, IInjector injector)
        {
            ConfigureCommon.With(config, new MugenInjectionObjectBuilder(injector));
            return config;
        }
    }
}
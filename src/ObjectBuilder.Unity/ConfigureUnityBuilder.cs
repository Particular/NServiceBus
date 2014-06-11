namespace NServiceBus
{
    using System;
    using Microsoft.Practices.Unity;

    /// <summary>
    /// Contains extension methods for configuring object builder infrastructure through Unity container.
    /// </summary>
    public static class ConfigureUnityBuilder
    {
        /// <summary>
        /// Use the Unity builder.
        /// </summary>
        [Obsolete("Replace with Configure.With(c=>.UseContainer<UnityObjectBuilder>())", true)]
// ReSharper disable UnusedParameter.Global
        public static Configure UnityBuilder(this Configure config)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Use the Unity builder passing in a pre-configured container to be used by nServiceBus.
        /// </summary>
        [Obsolete("Replace with Configure.With(c=>.UseContainer(new UnityObjectBuilder(container)))", true)]
// ReSharper disable UnusedParameter.Global
        public static Configure UnityBuilder(this Configure config, IUnityContainer container)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }
    }
}

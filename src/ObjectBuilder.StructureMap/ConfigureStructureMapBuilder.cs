namespace NServiceBus
{
    using System;
    using StructureMap;

    /// <summary>
    /// Contains extension methods to <see cref="Configure"/>.
    /// </summary>
    public static class ConfigureStructureMapBuilder
    {
        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [Obsolete("Replace with Configure.With(c=>.UseContainer<StructureMapObjectBuilder>())", true)]
// ReSharper disable UnusedParameter.Global
        public static Configure StructureMapBuilder(this Configure config)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="config"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        [Obsolete("Replace with Configure.With(c=>.UseContainer(new StructureMapObjectBuilder()))", true)]
// ReSharper disable UnusedParameter.Global
        public static Configure StructureMapBuilder(this Configure config, IContainer container)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException();

        }
    }
}


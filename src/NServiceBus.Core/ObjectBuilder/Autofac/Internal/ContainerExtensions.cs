namespace NServiceBus.ObjectBuilder.Autofac.Internal
{
    using System;
    using System.Collections.Generic;
    using global::Autofac;

    ///<summary>
    /// Autofac IContainer extensions
    ///</summary>
    internal static class ContainerExtensions
    {
        ///<summary>
        /// Resolve all components registered for the type.
        ///</summary>
        ///<param name="container"></param>
        ///<param name="componentType"></param>
        ///<returns></returns>
        public static IEnumerable<object> ResolveAll(this IComponentContext container, Type componentType)
        {
            return container.Resolve(typeof(IEnumerable<>).MakeGenericType(componentType)) as IEnumerable<object>;
        }
    }
}
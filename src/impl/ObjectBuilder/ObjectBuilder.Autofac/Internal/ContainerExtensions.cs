using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;

namespace NServiceBus.ObjectBuilder.Autofac.Internal
{
    ///<summary>
    /// Autofac IContainer extensions
    ///</summary>
    internal static class ContainerExtensions
    {
        ///<summary>
        /// Retrieve all registrations across container hierarchies as a single sequence
        ///</summary>
        ///<param name="container"></param>
        ///<returns></returns>
        public static IEnumerable<IComponentRegistration> GetAllRegistrations(this IContainer container)
        {
            var registrations = container.ComponentRegistrations;

            while (container.OuterContainer != null)
            {
                container = container.OuterContainer;
                registrations = registrations.Union(container.ComponentRegistrations);
            }

            return registrations;
        }

        ///<summary>
        /// Resolve all components registered for the type.
        ///</summary>
        ///<param name="container"></param>
        ///<param name="componentType"></param>
        ///<returns></returns>
        public static IEnumerable<object> ResolveAll(this IContainer container, Type componentType)
        {
            return container.Resolve(typeof(IEnumerable<>).MakeGenericType(componentType)) as IEnumerable<object>;
        }
    }
}
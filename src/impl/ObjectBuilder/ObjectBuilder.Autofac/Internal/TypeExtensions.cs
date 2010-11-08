using System;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.ObjectBuilder.Autofac.Internal
{
    ///<summary>
    /// Type extensions for Autofac
    ///</summary>
    internal static class TypeExtensions
    {
        ///<summary>
        /// Collect all interfaces implemented by a given type
        ///</summary>
        ///<param name="type"></param>
        ///<returns></returns>
        public static IEnumerable<Type> GetAllServices(this Type type)
        {
            if (type == null)
            {
                return new List<Type>();
            }

            var result = new List<Type>(type.GetInterfaces()) {
                type
            };

            foreach (Type interfaceType in type.GetInterfaces())
            {
                result.AddRange(GetAllServices(interfaceType));
            }

            return result.Distinct();
        }
    }
}
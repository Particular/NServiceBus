using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NServiceBus.Saga
{   
    /// <summary>
    /// Used to specify that a saga property should be unique across all saga instances. 
    /// This will ensure that 2 saga instances don't get persisted when using the property to correlate between multiple message types
    /// </summary>
    public class UniqueAttribute : Attribute
    {
        /// <summary>
        /// Gets all the properties that are marked with the UniqueAttribute for a saga entity
        /// </summary>
        /// <param name="entity">A saga entity</param>
        /// <returns>A dictionary of property names and their values</returns>
        public static IDictionary<string, object> GetUniqueProperties(ISagaEntity entity)
        {
            return GetUniqueProperties(entity.GetType()).ToDictionary(p => p.Name, p => p.GetValue(entity, null));
        }

        /// <summary>
        /// Gets all the properties that are marked with the UniqueAttribute for the given Type
        /// </summary>
        /// <param name="type">The type to evaluate</param>
        /// <returns>A queryable of PropertyInfo</returns>
        public static IQueryable<PropertyInfo> GetUniqueProperties(Type type)
        {
            return type.GetProperties()
                .Where(p => p.CanRead && p.GetCustomAttributes(typeof (UniqueAttribute), false).Length > 0).AsQueryable();
        }
    }
}
namespace NServiceBus.Saga
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Used to specify that a saga property should be unique across all saga instances. 
    /// This will ensure that 2 saga instances don't get persisted when using the property to correlate between multiple message types
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class UniqueAttribute : Attribute
    {
        /// <summary>
        /// Gets a single property that is marked with the UniqueAttribute for a saga entity
        /// </summary>
        /// <param name="type">The type to evaluate</param>
        /// <returns>A PropertyInfo of the property marked with a UniqAttribute or null if not used</returns>
        public static PropertyInfo GetUniqueProperty(Type type)
        {
            var properties = GetUniqueProperties(type);

            if (properties.Count() > 1)
                throw new InvalidOperationException(
                    string.Format("More than one UniqueAttribute property was found on the type '{0}'. However, only one property is supported.", type.FullName));

            return properties.SingleOrDefault();
        }

        /// <summary>
        /// Gets a single property that is marked with the UniqueAttribute for a saga entity
        /// </summary>
        /// <param name="entity">A saga entity</param>
        /// <returns>A PropertyInfo of the property marked with a UniqAttribute or null if not used</returns>
        public static KeyValuePair<string, object>? GetUniqueProperty(IContainSagaData entity)
        {
            var prop = GetUniqueProperty(entity.GetType());

            return prop != null ? 
                new KeyValuePair<string, object>(prop.Name, prop.GetValue(entity, null)) : 
                (KeyValuePair<string, object>?) null;
        }

        /// <summary>
        /// Gets all the properties that are marked with the UniqueAttribute for a saga entity
        /// </summary>
        /// <param name="entity">A saga entity</param>
        /// <returns>A dictionary of property names and their values</returns>
        public static IDictionary<string, object> GetUniqueProperties(IContainSagaData entity)
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
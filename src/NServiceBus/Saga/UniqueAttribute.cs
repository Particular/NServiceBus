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
        /// Gets a single property that is marked with the <see cref="UniqueAttribute"/> for a <see cref="IContainSagaData"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to evaluate.</param>
        /// <returns>A <see cref="PropertyInfo"/> of the property marked with a <see cref="UniqueAttribute"/> or null if not used.</returns>
        public static PropertyInfo GetUniqueProperty(Type type)
        {
            var properties = GetUniqueProperties(type);

            if (properties.Count() > 1)
            {
                var message = string.Format("More than one UniqueAttribute property was found on the type '{0}'. However, only one property is supported.", type.FullName);
                throw new InvalidOperationException(message);
            }

            return properties.SingleOrDefault();
        }

        /// <summary>
        /// Gets a single property that is marked with the <see cref="UniqueAttribute"/> for a saga entity.
        /// </summary>
        /// <param name="entity">A saga entity.</param>
        /// <returns>A <see cref="PropertyInfo"/> of the property marked with a <see cref="UniqueAttribute"/> or null if not used.</returns>
        public static KeyValuePair<string, object>? GetUniqueProperty(IContainSagaData entity)
        {
            var prop = GetUniqueProperty(entity.GetType());

            return prop != null ? 
                new KeyValuePair<string, object>(prop.Name, prop.GetValue(entity, null)) : 
                (KeyValuePair<string, object>?) null;
        }

        /// <summary>
        /// Gets all the properties that are marked with the <see cref="UniqueAttribute"/> for a saga entity.
        /// </summary>
        /// <param name="entity">A <see cref="IContainSagaData"/>.</param>
        /// <returns>A <see cref="IDictionary{TKey,TValue}"/> of property names and their values.</returns>
        public static IDictionary<string, object> GetUniqueProperties(IContainSagaData entity)
        {
            return GetUniqueProperties(entity.GetType())
                .ToDictionary(p => p.Name, p => p.GetValue(entity, null));
        }

        /// <summary>
        /// Gets all the properties that are marked with the <see cref="UniqueAttribute"/> for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns>A <see cref="IQueryable"/> of <see cref="PropertyInfo"/>.</returns>
        public static IQueryable<PropertyInfo> GetUniqueProperties(Type type)
        {
            return type.GetProperties()
                .Where(p => p.CanRead && p.GetCustomAttributes(typeof (UniqueAttribute), false).Length > 0)
                .AsQueryable();
        }
    }
}

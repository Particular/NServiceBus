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
    [AttributeUsage(AttributeTargets.Property)]
    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "There is no need for this attribute anymore, all mapped properties are automatically correlated.")]
    public sealed class UniqueAttribute : Attribute
    {
        /// <summary>
        /// Gets a single property that is marked with the <see cref="UniqueAttribute"/> for a <see cref="IContainSagaData"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to evaluate.</param>
        /// <returns>A <see cref="PropertyInfo"/> of the property marked with a <see cref="UniqueAttribute"/> or null if not used.</returns>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Use the new SagaMetadata")]
        public static PropertyInfo GetUniqueProperty(Type type)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a single property that is marked with the <see cref="UniqueAttribute"/> for a <see cref="IContainSagaData"/>.
        /// </summary>
        /// <param name="entity">A saga entity.</param>
        /// <returns>A <see cref="PropertyInfo"/> of the property marked with a <see cref="UniqueAttribute"/> or null if not used.</returns>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Use the new SagaMetadata")]
        public static KeyValuePair<string, object>? GetUniqueProperty(IContainSagaData entity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all the properties that are marked with the <see cref="UniqueAttribute"/> for a <see cref="IContainSagaData"/>.
        /// </summary>
        /// <param name="entity">A <see cref="IContainSagaData"/>.</param>
        /// <returns>A <see cref="IDictionary{TKey,TValue}"/> of property names and their values.</returns>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Use the new SagaMetadata")]
        public static IDictionary<string, object> GetUniqueProperties(IContainSagaData entity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all the properties that are marked with the <see cref="UniqueAttribute"/> for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns>A <see cref="IQueryable"/> of <see cref="PropertyInfo"/>.</returns>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Use the new SagaMetadata")]
        public static IEnumerable<PropertyInfo> GetUniqueProperties(Type type)
        {
            throw new NotImplementedException();
        }
    }
}

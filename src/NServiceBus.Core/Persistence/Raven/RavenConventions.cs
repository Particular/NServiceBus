namespace NServiceBus.Persistence.Raven
{
    using System;
    using Saga;

    /// <summary>
    /// NServiceBus default RavenDB conventions.
    /// </summary>
    public class RavenConventions
    {
        /// <summary>
        /// NServiceBus default RavenDB FindTypeTagName convention
        /// </summary>
        /// <param name="t">The type to apply convention.</param>
        /// <returns>The name of the find type tag.</returns>
        public static string FindTypeTagName(Type t)
        {
            var tagName = t.Name;

            if (IsASagaEntity(t))
            {
                tagName = tagName.Replace("Data", String.Empty);
            }

            return tagName;
        }

        static bool IsASagaEntity(Type t)
        {
            return t != null && typeof(IContainSagaData).IsAssignableFrom(t);
        }
    }
}
namespace NServiceBus.Persistence.Raven
{
    using System;
    using Saga;

    class RavenConventions
    {
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
            return t != null && typeof(ISagaEntity).IsAssignableFrom(t);
        }
    }
}
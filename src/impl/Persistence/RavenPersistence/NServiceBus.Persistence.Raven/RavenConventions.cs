using System;
using NServiceBus.Saga;

namespace NServiceBus.Persistence.Raven
{
    public class RavenConventions
    {
        public string FindTypeTagName(Type t)
        {
            var tagName = t.Name;

            if (IsASagaEntity(t))
                tagName = tagName.Replace("Data", "");

            return tagName;
        }

        static bool IsASagaEntity(Type t)
        {
            return t != null && typeof(ISagaEntity).IsAssignableFrom(t);
        }
    }
}
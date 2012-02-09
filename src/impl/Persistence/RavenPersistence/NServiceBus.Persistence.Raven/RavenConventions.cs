using System;
using System.Reflection;
using NServiceBus.Saga;

namespace NServiceBus.Persistence.Raven
{
    public class RavenConventions
    {
        public bool FindIdentityProperty(PropertyInfo prop)
        {
            Func<PropertyInfo, bool> defaultIdConvention = q => q.Name == "Id";

            if (!IsASagaEntity(prop.DeclaringType))
                return defaultIdConvention(prop);

            var uniqueProperty = UniqueAttribute.GetUniqueProperty(prop.DeclaringType);

            return uniqueProperty == null ?
                defaultIdConvention(prop) :
                uniqueProperty.Equals(prop);
        }

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
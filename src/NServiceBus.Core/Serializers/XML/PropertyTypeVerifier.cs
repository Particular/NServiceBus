namespace NServiceBus.Serializers.XML
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    static class PropertyTypeVerifier
    {

        public static void VerifyCanBeSerialized(this PropertyInfo prop)
        {
            if (typeof(IList) == prop.PropertyType)
            {
                throw new NotSupportedException("IList is not a supported property type for serialization, use List instead. Type: " + prop.DeclaringType.FullName + " Property: " + prop.Name);
            }

            var args = prop.PropertyType.GetGenericArguments();

            if (args.Length == 1)
            {
                if (typeof(IList<>).MakeGenericType(args) == prop.PropertyType)
                {
                    throw new NotSupportedException("IList<T> is not a supported property type for serialization, use List<T> instead. Type: " + prop.DeclaringType.FullName + " Property: " + prop.Name);
                }
                if (typeof(ISet<>).MakeGenericType(args) == prop.PropertyType)
                {
                    throw new NotSupportedException("ISet<T> is not a supported property type for serialization, use HashSet<T> instead. Type: " + prop.DeclaringType.FullName + " Property: " + prop.Name);
                }
            }

            if (args.Length == 2)
            {
                if (typeof(IDictionary<,>).MakeGenericType(args) == prop.PropertyType)
                {
                    throw new NotSupportedException("IDictionary<T, K> is not a supported property type for serialization, use Dictionary<T,K> instead. Type: " + prop.DeclaringType.FullName + " Property: " + prop.Name + ". Consider using a concrete Dictionary<T, K> instead, where T and K cannot be of type 'System.Object'");
                }

                if (args[0].FullName == "System.Object" || args[1].FullName == "System.Object")
                {
                    throw new NotSupportedException("Dictionary<T, K> is not a supported when Key or Value is of Type System.Object. Type: " + prop.DeclaringType.FullName + " Property: " + prop.Name + ". Consider using a concrete Dictionary<T, K> where T and K are not of type 'System.Object'");
                }
            }
        }
    }
}
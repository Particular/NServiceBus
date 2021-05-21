namespace NServiceBus.Core.Tests.API.Infra
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    static partial class TypeExtensions
    {
        public static bool IsObsolete(this Type type, Stack<Type> seenTypes = null)
        {
            if (seenTypes == null)
            {
                seenTypes = new Stack<Type>();
            }

            if (seenTypes.Contains(type))
            {
                return false;
            }

            seenTypes.Push(type);
            try
            {
                return type.GetCustomAttributes<ObsoleteAttribute>(true).Any() ||
                    type.GetGenericArguments().Any(arg => arg.IsObsolete(seenTypes)) ||
                    (type.DeclaringType != null && type.DeclaringType.IsObsolete(seenTypes));
            }
            finally
            {
                _ = seenTypes.Pop();
            }
        }

        public static bool IsStatic(this Type type) =>
            type.IsAbstract && type.IsSealed;
    }
}

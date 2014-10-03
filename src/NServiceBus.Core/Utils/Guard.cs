namespace NServiceBus.Utils
{
    using System;
    using System.Linq;
    using System.Reflection;

    static class Guard
    {
        public static void TypeHasDefaultConstructor(Type type, string argumentName)
        {
            if (type.GetConstructors(BindingFlags.Instance).All(ctor => ctor.GetParameters().Length != 0))
                throw new ArgumentException(String.Format("Type '{0}' must have a default constructor.", type.FullName), argumentName);
        }
    }
}
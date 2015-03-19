namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    static class Guard
    {
        public static void TypeHasDefaultConstructor(Type type, string argumentName)
        {
            if (type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .All(ctor => ctor.GetParameters().Length != 0))
                throw new ArgumentException(String.Format("Type '{0}' must have a default constructor.", type.FullName), argumentName);
        }

        // ReSharper disable UnusedParameter.Global
        public static void AgainstDefault<T>(T value, string argumentName)
        {
            if (EqualityComparer<T>.Default.Equals(value,default(T)))
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        public static void AgainstDefaultOrEmpty(string value, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(argumentName);
            }
        }
        public static void AgainstDefaultOrEmpty(ICollection value, string argumentName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argumentName);
            }
            if (value.Count == 0)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }

        public static void AgainstLessThanOrEqualZero(int value, string argumentName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }
        public static void AgainstNegative(int value, string argumentName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }

        public static void AgainstLessThanOrEqualZero(TimeSpan? value, string argumentName)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }
        public static void AgainstNegative(TimeSpan? value, string argumentName)
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }
    }
}
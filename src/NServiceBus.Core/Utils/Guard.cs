namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;

    static class Guard
    {
        public static void TypeHasDefaultConstructor(Type type, string argumentName)
        {
            if (type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .All(ctor => ctor.GetParameters().Length != 0))
            {
                var error = string.Format("Type '{0}' must have a default constructor.", type.FullName);
                throw new ArgumentException(error, argumentName);
            }
        }

        // ReSharper disable UnusedParameter.Global
        public static void AgainstNull(object value, string argumentName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        public static void AgainstNullAndEmpty(string value, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(argumentName);
            }
        }
        public static void AgainstNullAndEmpty(ICollection value, string argumentName)
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

        public static void AgainstNegativeAndZero(int value, string argumentName)
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

        public static void AgainstNegativeAndZero(TimeSpan? value, string argumentName)
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
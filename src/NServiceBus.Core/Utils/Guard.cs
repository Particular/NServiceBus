namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;

    static class Guard
    {
        // ReSharper disable UnusedParameter.Global
        public static void TypeHasDefaultConstructor(Type type, string argumentName)
        {
            if (type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .All(ctor => ctor.GetParameters().Length != 0))
            {
                var error = $"Type '{type.FullName}' must have a default constructor.";
                throw new ArgumentException(error, argumentName);
            }
        }

        public static void AgainstNull(string argumentName, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        public static void AgainstNullAndEmpty(string argumentName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        public static void AgainstNullAndEmpty(string argumentName, ICollection value)
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

        public static void AgainstNegativeAndZero(string argumentName, int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }

        public static void AgainstNegative(string argumentName, int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }

        public static void AgainstNegativeAndZero(string argumentName, TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }

        public static void AgainstNegative(string argumentName, TimeSpan value)
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }
    }
}
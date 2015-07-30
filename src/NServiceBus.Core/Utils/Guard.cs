namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using JetBrains.Annotations;

    static class Guard
    {
        // ReSharper disable UnusedParameter.Global
        public static void TypeHasDefaultConstructor(Type type, [InvokerParameterName] string argumentName)
        {
            if (type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .All(ctor => ctor.GetParameters().Length != 0))
            {
                var error = string.Format("Type '{0}' must have a default constructor.", type.FullName);
                throw new ArgumentException(error, argumentName);
            }
        }
        
        [ContractAnnotation("value: null => halt")]
        public static void AgainstNull([NotNull] object value, [InvokerParameterName] string argumentName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        [ContractAnnotation("value: null => halt")]
        public static void AgainstNullAndEmpty([NotNull] string value, [InvokerParameterName] string argumentName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        [ContractAnnotation("value: null => halt")]
        public static void AgainstNullAndEmpty([NotNull, NoEnumeration] ICollection value, [InvokerParameterName] string argumentName)
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

        public static void AgainstNegativeAndZero(int value, [InvokerParameterName] string argumentName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }
        public static void AgainstNegative(int value, [InvokerParameterName] string argumentName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }

        public static void AgainstNegativeAndZero(TimeSpan? value, [InvokerParameterName] string argumentName)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }

        public static void AgainstNegative(TimeSpan? value, [InvokerParameterName] string argumentName)
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }
    }
}
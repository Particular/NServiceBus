namespace NServiceBus.Pipeline
{
    using System;
    using System.Linq;

    static class BehaviorTypeChecker
    {
        public static void ThrowIfInvalid(Type behavior, string paramName)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException(paramName);
            }
            //if (behavior.IsAbstract)
            //{
            //    throw new ArgumentException(string.Format("The behavior '{0}' is invalid since it is abstract.", behavior.Name), paramName);
            //}
            if (behavior.IsGenericTypeDefinition)
            {
                throw new ArgumentException(string.Format("The behavior '{0}' is invalid since it is an open generic.", behavior.Name), paramName);
            }
            if (!IsAssignableToIBehavior(behavior))
            {
                throw new ArgumentException(string.Format("The behavior '{0}' is invalid since it does not implement IBehavior<T>.", behavior.Name), paramName);
            }
        }

        static Type iBehaviorType = typeof(IBehavior<,>);

        static bool IsAssignableToIBehavior(Type givenType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            if (interfaceTypes.Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == iBehaviorType))
            {
                return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == iBehaviorType)
                return true;

            var baseType = givenType.BaseType;
            if (baseType == null) return false;

            return IsAssignableToIBehavior(baseType);
        }
    }
}
namespace NServiceBus.Pipeline
{
    using System;
    using System.Linq;

    static class BehaviorTypeChecker
    {

        public static void ThrowIfInvalid(Type behavior)
        {
            if (behavior == null)
            {
                throw new Exception("Behavior cannot be null.");
            }
            if (behavior.IsAbstract)
            {
                throw new Exception(string.Format("The behavior '{0}' is invalid since it is abstract.", behavior.Name));
            }
            if (behavior.IsGenericTypeDefinition)
            {
                throw new Exception(string.Format("The behavior '{0}' is invalid since it is an open generic.", behavior.Name));
            }
            if (!IsAssignableToIBehavior(behavior))
            {
                throw new Exception(string.Format("The behavior '{0}' is invalid since it does not implement IBehavior<T>.", behavior.Name));
            }
        }

        static Type iBehaviorType = typeof(IBehavior<>);

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
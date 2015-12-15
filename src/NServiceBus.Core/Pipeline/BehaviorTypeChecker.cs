namespace NServiceBus
{
    using System;

    static class BehaviorTypeChecker
    {
        public static void ThrowIfInvalid(Type behavior, string paramName)
        {
            Guard.AgainstNull(nameof(behavior), behavior);
            if (behavior.IsAbstract)
            {
                throw new ArgumentException($"The behavior '{behavior.Name}' is invalid since it is abstract.", paramName);
            }
            if (behavior.IsGenericTypeDefinition)
            {
                throw new ArgumentException($"The behavior '{behavior.Name}' is invalid since it is an open generic.", paramName);
            }
            if (!behavior.IsBehavior())
            {
                throw new ArgumentException($@"The behavior '{behavior.Name}' is invalid since it does not implement IBehavior<TFrom, TTo>.", paramName);
            }

            var inputContextType = behavior.GetInputContext();
            if (!inputContextType.IsInterface)
            {
                throw new ArgumentException($@"The behavior '{behavior.Name}' is invalid since the TFrom context of IBehavior<TFrom, TTo> is not an interface.", paramName);
            }

            var outputContextType = behavior.GetOutputContext();
            if (!outputContextType.IsInterface)
            {
                throw new ArgumentException($@"The behavior '{behavior.Name}' is invalid since the TTo context of IBehavior<TFrom, TTo> is not an interface.", paramName);
            }
        }
    }
}
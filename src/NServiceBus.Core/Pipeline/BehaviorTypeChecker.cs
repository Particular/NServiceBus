namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Pipeline;

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
            if (NotAllowedInterfaces.Contains(inputContextType))
            {
                throw new ArgumentException($@"The behavior '{behavior.Name}' is invalid since the TFrom {inputContextType} context of IBehavior<TFrom, TTo> is not intended to be used.", paramName);
            }

            var outputContextType = behavior.GetOutputContext();
            if (NotAllowedInterfaces.Contains(outputContextType))
            {
                throw new ArgumentException($@"The behavior '{behavior.Name}' is invalid since the TTo {outputContextType} context of IBehavior<TFrom, TTo> is not intended to be used.", paramName);
            }
        }

        static HashSet<Type> NotAllowedInterfaces = new HashSet<Type>
        {
            typeof(IBehaviorContext),
            typeof(IIncomingContext),
            typeof(IOutgoingContext)
        };
    }
}
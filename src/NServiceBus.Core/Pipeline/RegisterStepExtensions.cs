namespace NServiceBus
{
    using System;
    using System.Linq;
    using Pipeline;

    static class RegisterStepExtensions
    {
        public static bool IsStageConnector(this RegisterStep step)
        {
            return typeof(IStageConnector).IsAssignableFrom(step.BehaviorType);
        }

        public static Type GetContextType(this Type behaviorType)
        {
            var behaviorInterface = behaviorType.GetBehaviorInterface();
            return behaviorInterface.GetGenericArguments()[0];
        }

        public static bool IsBehavior(this Type behaviorType)
        {
            return behaviorType.GetInterfaces()
                .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == BehaviorInterfaceType);
        }

        public static Type GetBehaviorInterface(this Type behaviorType)
        {
            return behaviorType.GetInterfaces()
                .First(x => x.IsGenericType && x.GetGenericTypeDefinition() == BehaviorInterfaceType);
        }

        public static Type GetOutputContext(this RegisterStep step)
        {
            return step.BehaviorType.GetOutputContext();
        }

        public static Type GetOutputContext(this Type behaviorType)
        {
            var behaviorInterface = GetBehaviorInterface(behaviorType);
            return behaviorInterface.GetGenericArguments()[1];
        }

        public static Type GetInputContext(this RegisterStep step)
        {
            return step.BehaviorType.GetInputContext();
        }

        public static Type GetInputContext(this Type behaviorType)
        {
            var behaviorInterface = GetBehaviorInterface(behaviorType);
            return behaviorInterface.GetGenericArguments()[0];
        }

        static Type BehaviorInterfaceType = typeof(IBehavior<,>);
    }
}
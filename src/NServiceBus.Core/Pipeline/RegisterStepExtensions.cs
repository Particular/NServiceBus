#nullable enable

namespace NServiceBus;

using System;
using System.Linq;
using Pipeline;

static class RegisterStepExtensions
{
    public static bool IsBehavior(this Type behaviorType) =>
        behaviorType.GetInterfaces()
            .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == BehaviorInterfaceType);

    public static Type GetBehaviorInterface(this Type behaviorType) =>
        behaviorType.GetInterfaces()
            .First(x => x.IsGenericType && x.GetGenericTypeDefinition() == BehaviorInterfaceType);

    public static Type GetOutputContext(this Type behaviorType)
    {
        var behaviorInterface = behaviorType.GetBehaviorInterface();
        return behaviorInterface.GetGenericArguments()[1];
    }

    public static Type GetInputContext(this Type behaviorType)
    {
        var behaviorInterface = behaviorType.GetBehaviorInterface();
        return behaviorInterface.GetGenericArguments()[0];
    }

    static readonly Type BehaviorInterfaceType = typeof(IBehavior<,>);
}
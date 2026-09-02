#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Pipeline;

static class RegisterStepExtensions
{
    public static bool IsBehavior([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type behaviorType) =>
        behaviorType.GetInterfaces()
            .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == BehaviorInterfaceType);

    public static Type GetBehaviorInterface([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type behaviorType) =>
        behaviorType.GetInterfaces()
            .First(x => x.IsGenericType && x.GetGenericTypeDefinition() == BehaviorInterfaceType);

    public static Type GetOutputContext([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type behaviorType)
    {
        var behaviorInterface = behaviorType.GetBehaviorInterface();
        return behaviorInterface.GetGenericArguments()[1];
    }

    public static Type GetInputContext([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type behaviorType)
    {
        var behaviorInterface = behaviorType.GetBehaviorInterface();
        return behaviorInterface.GetGenericArguments()[0];
    }

    static readonly Type BehaviorInterfaceType = typeof(IBehavior<,>);
}
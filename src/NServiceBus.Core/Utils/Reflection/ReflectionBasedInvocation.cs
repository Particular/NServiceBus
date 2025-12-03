#nullable enable
namespace NServiceBus;

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

sealed class ReflectionBasedInvocation(MethodInfo method)
{
    public object? InvokeGeneric(Func<MethodInfo, object?> invocation, params Type[] genericTypes)
    {
        try
        {
            return invocation(method.MakeGenericMethod(genericTypes));
        }
        catch (TargetInvocationException e)
        {
            if (e.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }

            var genericParameters = string.Join(",", genericTypes.Select(t => t.Name));
            throw new Exception($"Failed invoke {method.Name}<{genericParameters}> using reflection", e);
        }
    }
}
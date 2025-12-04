#nullable enable

namespace NServiceBus;

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

static class MethodInfoExtensions
{
    public static object? InvokeGeneric(this MethodInfo method, object? target, object?[]? args, params Type[] genericTypes)
    {
        try
        {
            return method.MakeGenericMethod(genericTypes).Invoke(target, args);
        }
        catch (TargetInvocationException e)
        {
            if (e.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }

            var genericParameters = string.Join(",", genericTypes.Select(t => t.Name));
            throw new Exception($"Failed to invoke {method.Name}<{genericParameters}> using reflection", e);
        }
    }
}
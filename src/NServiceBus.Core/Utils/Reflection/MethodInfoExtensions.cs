#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

static class MethodInfoExtensions
{
    extension(MethodInfo method)
    {
        [RequiresDynamicCode(DynamicCodeMessage)]
        [RequiresUnreferencedCode(TrimmingMessage)]
        public T? InvokeGeneric<T>(object? target, object?[]? args, Type[] genericTypes) => (T?)method.InvokeGeneric(target, args, genericTypes);

        [RequiresDynamicCode(DynamicCodeMessage)]
        [RequiresUnreferencedCode(TrimmingMessage)]
        public T? InvokeGeneric<T>(object?[]? args, Type[] genericTypes) => (T?)method.InvokeGeneric(null, args, genericTypes);

        [RequiresDynamicCode(DynamicCodeMessage)]
        [RequiresUnreferencedCode(TrimmingMessage)]
        public T? InvokeGeneric<T>(Type genericType) => (T?)method.InvokeGeneric(null, null, [genericType]);

        [RequiresDynamicCode(DynamicCodeMessage)]
        [RequiresUnreferencedCode(TrimmingMessage)]
        public object? InvokeGeneric(object? target, Type[] genericTypes) => method.InvokeGeneric(target, null, genericTypes);

        [RequiresDynamicCode(DynamicCodeMessage)]
        [RequiresUnreferencedCode(TrimmingMessage)]
        public object? InvokeGeneric(object? target, object?[]? args, Type[] genericTypes)
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

    const string TrimmingMessage = "Generic invocations might require access to unreferenced code";
    const string DynamicCodeMessage = "Generic invocation relies on dynamic code generation which is not available with Ahead of Time compilation";
}
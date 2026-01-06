#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

static class MethodInfoExtensions
{
    const string TrimWarning = "This method calls MakeGenericMethod and cannot be used in trimming.";
    extension(MethodInfo method)
    {
        [RequiresUnreferencedCode(TrimWarning)]
        public T? InvokeGeneric<T>(object? target, object?[]? args, Type[] genericTypes) => (T?)method.InvokeGeneric(target, args, genericTypes);

        [RequiresUnreferencedCode(TrimWarning)]
        public T? InvokeGeneric<T>(object?[]? args, Type[] genericTypes) => (T?)method.InvokeGeneric(null, args, genericTypes);

        [RequiresUnreferencedCode(TrimWarning)]
        public T? InvokeGeneric<T>(Type genericType) => (T?)method.InvokeGeneric(null, null, [genericType]);

        [RequiresUnreferencedCode(TrimWarning)]
        public object? InvokeGeneric(object? target, Type[] genericTypes) => method.InvokeGeneric(target, null, genericTypes);

        [RequiresUnreferencedCode(TrimWarning)]
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
}
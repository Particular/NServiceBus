namespace NServiceBus.Core.Tests.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Core.Tests.API.Infra;
    using NUnit.Framework;

    static class TaskReturningDelegateParameters
    {
        static readonly List<(MethodInfo Method, ParameterInfo Parameter, ParameterInfo[] InvokeParameters)> parameters =
            NServiceBusAssembly.Methods
                .SelectMany(method => method.GetParameters()
                    .Where(param => param.ParameterType != typeof(Delegate) && typeof(Delegate).IsAssignableFrom(param.ParameterType))
                    .Select(param => (Parameter: param, InvokeMethod: param.ParameterType.GetMethod("Invoke")))
                    .Where(param => typeof(Task).IsAssignableFrom(param.InvokeMethod.ReturnType))
                    .Select(param => (method, param.Parameter, param.InvokeMethod.GetParameters())))
                    .ToList();

        [Test]
        public static void HaveCancellationTokens()
        {
            var violators = parameters
                .Where(param => !typeof(NServiceBus.Pipeline.IBehavior).IsAssignableFrom(param.Method.DeclaringType))
                .Where(param => !param.InvokeParameters.Any(p => p.ParameterType == typeof(CancellationToken)))
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void HaveAtMostOnceCancellationToken()
        {
            var violators = parameters
                .Where(param => param.InvokeParameters.Count(p => p.ParameterType == typeof(CancellationToken)) > 1)
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void HaveCancellationTokensLast()
        {
            var violators = parameters
                .Where(param =>
                    param.InvokeParameters.Any(p => p.ParameterType == typeof(CancellationToken)) &&
                    param.InvokeParameters.Last().ParameterType != typeof(CancellationToken))
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }

        static IEnumerable<string> Prettify(this IEnumerable<(MethodInfo Method, ParameterInfo Parameter, ParameterInfo[] InvokeParameters)> parameters) =>
            parameters
                .OrderBy(param => param.Method, MethodInfoComparer.Instance)
                .ThenBy(param => param.Parameter.Name)
                .Select(param => $"{param.Method.DeclaringType.FullName} {{ {param.Method} ({param.Parameter.Name}) }}");
    }
}

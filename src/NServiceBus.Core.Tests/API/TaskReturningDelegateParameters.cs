namespace NServiceBus.Core.Tests.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using NServiceBus.Core.Tests.API.Infra;
    using NUnit.Framework;

    static class TaskReturningDelegateParameters
    {
        static readonly List<(MethodBase MethodBase, ParameterInfo Parameter, ParameterInfo[] InvokeParameters)> parameters =
            NServiceBusAssembly.MethodsAndConstructors
                .SelectMany(methodBase => methodBase.GetParameters()
                    .Where(param => param.ParameterType != typeof(Delegate) && typeof(Delegate).IsAssignableFrom(param.ParameterType))
                    .Select(param => (Parameter: param, InvokeMethod: param.ParameterType.GetMethod("Invoke")))
                    .Where(param => typeof(Task).IsAssignableFrom(param.InvokeMethod.ReturnType))
                    .Select(param => (methodBase, param.Parameter, param.InvokeMethod.GetParameters())))
                .ToList();

        [Test]
        public static void HaveCancellationTokens()
        {
            var violators = parameters
                .Where(param => !param.MethodBase.DeclaringType.IsBehavior() && !param.InvokeParameters.CancellableContexts().Any())
                .Where(param => !param.InvokeParameters.CancellationTokens().Any())
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void DoNotHaveCancellationTokens()
        {
            var violators = parameters
                .Where(param => param.MethodBase.DeclaringType.IsBehavior() || param.InvokeParameters.CancellableContexts().Any())
                .Where(param => param.InvokeParameters.CancellationTokens().Any())
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void HaveAtMostOnceCancellationToken()
        {
            var violators = parameters
                .Where(param => param.InvokeParameters.CancellationTokens().Count() > 1)
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
                    param.InvokeParameters.CancellationTokens().Any() &&
                    !param.InvokeParameters.Last().ParameterType.IsCancellationToken())
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }

        static IEnumerable<string> Prettify(this IEnumerable<(MethodBase MethodBase, ParameterInfo Parameter, ParameterInfo[] InvokeParameters)> parameters) =>
            parameters
                .OrderBy(param => param.MethodBase, MethodBaseComparer.Instance)
                .ThenBy(param => param.Parameter.Name)
                .Select(param => $"{param.MethodBase.DeclaringType.FullName} {{ {param.MethodBase} ({param.Parameter.Name}) }}");
    }
}

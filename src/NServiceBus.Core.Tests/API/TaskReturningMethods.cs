namespace NServiceBus.Core.Tests.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using NServiceBus.Core.Tests.API.Infra;
    using NUnit.Framework;

    static class TaskReturningMethods
    {
        static readonly List<MethodInfo> methods = NServiceBusAssembly.Methods
            .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType))
            .ToList();

        static readonly List<MethodInfo> noTokenMethods = methods
            .Where(method =>
                method.GetParameters().CancellableContexts().Any() ||
                method.IsOn(typeof(ICancellableContext)) ||
                (method.IsOn(typeof(TaskEx)) && method.Name == nameof(TaskEx.ThrowIfNull)) ||
                (method.IsOn(typeof(IEndpointInstanceExtensions)) &&
                    method.Name == nameof(IEndpointInstanceExtensions.Stop) &&
                    method.GetParameters().Any(parameter => parameter.ParameterType == typeof(TimeSpan))))
            .ToList();

        static readonly List<MethodInfo> mandatoryTokenMethods = methods
            .Where(method => !noTokenMethods.Contains(method))
            .Where(method => method.IsPrivate)
            .ToList();

        static readonly List<MethodInfo> optionalTokenMethods = methods
            .Where(method => !noTokenMethods.Contains(method))
            .Where(method => !mandatoryTokenMethods.Contains(method))
            .ToList();

        [TestCase(true)]
        [TestCase(false)]
        public static void EachHaveASingleTokenPolicy(bool visible)
        {
            var methodPolicies = methods
                .Where(method => method.IsVisible() == visible)
                .ToDictionary(method => method, method => new List<string>());

            RecordPolicy(noTokenMethods, nameof(noTokenMethods));
            RecordPolicy(mandatoryTokenMethods, nameof(mandatoryTokenMethods));
            RecordPolicy(optionalTokenMethods, nameof(optionalTokenMethods));

            var violators = methodPolicies
                .Where(pair => pair.Value.Count != 1)
                .OrderBy(pair => pair.Key, MethodBaseComparer.Instance)
                .Select(pair => $"Method: {pair.Key.Prettify()}, Policies: {(pair.Value.Count == 0 ? "(none)" : string.Join(", ", pair.Value))}")
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);

            void RecordPolicy(List<MethodInfo> policy, string name)
            {
                foreach (var method in policy.Where(method => method.IsVisible() == visible))
                {
                    methodPolicies[method].Add(name);
                }
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public static void HaveNoTokens(bool visible)
        {
            var violators = noTokenMethods
                .Where(method => method.IsVisible() == visible && method.GetParameters().CancellationTokens().Any())
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }

        [TestCase(true)]
        [TestCase(false)]
        public static void HaveOptionalTokens(bool visible)
        {
            var violators = optionalTokenMethods
                .Where(method => method.IsVisible() == visible && !method.GetParameters().CancellationTokens().Any(param => param.IsOptional))
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }

        [TestCase(true)]
        [TestCase(false)]
        public static void HaveMandatoryTokens(bool visible)
        {
            var violators = mandatoryTokenMethods
                .Where(method => method.IsVisible() == visible && !method.GetParameters().CancellationTokens().Any(param => !param.IsOptional))
                .Prettify()
                .ToList();

            Console.Error.WriteViolators(violators);

            Assert.IsEmpty(violators);
        }
    }
}

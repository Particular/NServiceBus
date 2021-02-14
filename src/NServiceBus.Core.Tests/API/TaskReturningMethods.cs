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

#pragma warning disable IDE0001 // Simplify Names
        static readonly List<MethodInfo> noTokenMethods = methods
            .Where(method =>
                (method.GetParameters().CancellableContexts().Any() &&
                    method.IsOn(
                        typeof(NServiceBus.Saga),
                        typeof(NServiceBus.IncomingMessageOperations),
                        typeof(NServiceBus.IHandleMessages<>),
                        typeof(NServiceBus.IHandleTimeouts<>),
                        typeof(NServiceBus.IPipeline),
                        typeof(NServiceBus.IUnicastPublishRouter),
                        typeof(NServiceBus.Sagas.IHandleSagaNotFound),
                        typeof(NServiceBus.Pipeline.MessageHandler),
                        typeof(NServiceBus.Pipeline.IBehavior),
                        typeof(NServiceBus.MessageMutator.IMutateIncomingMessages),
                        typeof(NServiceBus.MessageMutator.IMutateIncomingTransportMessages),
                        typeof(NServiceBus.MessageMutator.IMutateOutgoingMessages),
                        typeof(NServiceBus.MessageMutator.IMutateOutgoingTransportMessages))) ||
                method.IsOn(typeof(NServiceBus.ICancellableContext)) ||
                (method.IsOn(typeof(NServiceBus.TaskEx)) && method.Name == "ThrowIfNull"))
            .ToList();

        static readonly List<MethodInfo> optionalTokenMethods = methods
            .Where(method => !noTokenMethods.Contains(method))
            .Where(method => method.IsOn(
                typeof(NServiceBus.Endpoint),
                typeof(NServiceBus.IEndpointInstance),
                typeof(NServiceBus.IMessageSession),
                typeof(NServiceBus.IStartableEndpoint),
                typeof(NServiceBus.IStartableEndpointWithExternallyManagedContainer)))
            .ToList();
#pragma warning restore IDE0001 // Simplify Names

        static readonly List<MethodInfo> mandatoryTokenMethods = methods
            .Where(method => !noTokenMethods.Contains(method))
            .Where(method => !optionalTokenMethods.Contains(method))
            .ToList();

        [TestCase(true)]
        [TestCase(false)]
        public static void EachHaveASingleTokenPolicy(bool visible)
        {
            var methodPolicies = methods
                .Where(method => method.IsVisible() == visible)
                .ToDictionary(method => method, method => new List<string>());

            RecordPolicy(noTokenMethods, nameof(noTokenMethods));
            RecordPolicy(optionalTokenMethods, nameof(optionalTokenMethods));
            RecordPolicy(mandatoryTokenMethods, nameof(mandatoryTokenMethods));

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

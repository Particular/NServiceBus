namespace NServiceBus.Core.Tests.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    static class TaskReturning
    {
        static readonly List<MethodInfo> allMethods = typeof(IMessage).Assembly.GetTypes()
            .Where(type => !type.IsObsolete())
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(method => !method.IsCompilerGenerated())
            .Where(method => !method.IsObsolete())
            .ToList();

        static readonly List<MethodInfo> taskMethods = allMethods
            .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType))
            .ToList();

#pragma warning disable IDE0001 // Simplify Names
        static readonly List<MethodInfo> noTokenPolicy = taskMethods
            .Where(method =>
                (typeof(Delegate).IsAssignableFrom(method.DeclaringType) && method.Name == "EndInvoke") ||
                (method.HasCancellableContext() &&
                    method.IsOnOneOf(
                        typeof(NServiceBus.Saga),
                        typeof(NServiceBus.IncomingMessageOperations),
                        typeof(NServiceBus.IPipeline),
                        typeof(NServiceBus.Pipeline.IBehavior),
                        typeof(NServiceBus.IHandleMessages<>),
                        typeof(NServiceBus.IHandleTimeouts<>),
                        typeof(NServiceBus.Sagas.IHandleSagaNotFound),
                        typeof(NServiceBus.Pipeline.MessageHandler),
                        typeof(NServiceBus.IUnicastPublishRouter),
                        typeof(NServiceBus.MessageMutator.IMutateIncomingMessages),
                        typeof(NServiceBus.MessageMutator.IMutateIncomingTransportMessages),
                        typeof(NServiceBus.MessageMutator.IMutateOutgoingMessages),
                        typeof(NServiceBus.MessageMutator.IMutateOutgoingTransportMessages)
                        )) ||
                method.IsOn(typeof(NServiceBus.IAsyncTimer)) ||
                method.IsOn(typeof(NServiceBus.ICancellableContext)) ||
                method.IsOn(typeof(NServiceBus.DelayedMessagePoller)) ||
                (method.IsOn(typeof(NServiceBus.TaskEx)) && method.Name == "ThrowIfNull")) // TODO: Consider removing this type?
            .ToList();

        static readonly List<MethodInfo> optionalTokenPolicy = taskMethods
            .Where(method => !noTokenPolicy.Contains(method))
            .Where(method =>
                method.IsOn(typeof(NServiceBus.Endpoint)) ||
                method.IsOn(typeof(NServiceBus.IEndpointInstance)) ||
                method.IsOn(typeof(NServiceBus.IMessageSession)) ||
                method.IsOn(typeof(NServiceBus.IStartableEndpoint)) ||
                method.IsOn(typeof(NServiceBus.IStartableEndpointWithExternallyManagedContainer)))
            .ToList();
#pragma warning restore IDE0001 // Simplify Names

        static readonly List<MethodInfo> mandatoryTokenPolicy = taskMethods
            .Where(method => !noTokenPolicy.Contains(method))
            .Where(method => !optionalTokenPolicy.Contains(method))
            .ToList();

        [TestCase(true)]
        [TestCase(false)]
        public static void AllMethodsHaveASingleTokenPolicy(bool visible)
        {
            var methodPolicies = taskMethods
                .Where(method => method.IsVisible() == visible)
                .ToDictionary(method => method, method => new List<string>());

            foreach (var method in noTokenPolicy.Where(method => method.IsVisible() == visible))
            {
                methodPolicies[method].Add(nameof(noTokenPolicy));
            }

            foreach (var method in optionalTokenPolicy.Where(method => method.IsVisible() == visible))
            {
                methodPolicies[method].Add(nameof(optionalTokenPolicy));
            }

            foreach (var method in mandatoryTokenPolicy.Where(method => method.IsVisible() == visible))
            {
                methodPolicies[method].Add(nameof(mandatoryTokenPolicy));
            }

            var violators = methodPolicies
                .Where(pair => pair.Value.Count != 1)
                .Select(pair => pair.Key)
                .Sort()
                .Select(method => new { Method = method, Policies = methodPolicies[method] })
                .Select(pair => $"Method: {new MethodPrinter(pair.Method)}, Policies: {(pair.Policies.Count == 0 ? "(none)" : string.Join(", ", pair.Policies))}")
                .ToList();

            violators.Write(nameof(AllMethodsHaveASingleTokenPolicy), visible);

            Assert.IsEmpty(violators);
        }

        [TestCase(true)]
        [TestCase(false)]
        public static void NoTokenPolicy(bool visible)
        {
            var violators = noTokenPolicy
                .Where(method => method.IsVisible() == visible && method.GetParameters().Any(param => param.ParameterType == typeof(CancellationToken)))
                .Prettify()
                .ToList();

            violators.Write(nameof(NoTokenPolicy), visible);

            Assert.IsEmpty(violators);
        }

        [TestCase(true)]
        [TestCase(false)]
        public static void OptionalTokenPolicy(bool visible)
        {
            var violators = optionalTokenPolicy
                .Where(method => method.IsVisible() == visible && !method.GetParameters().Any(param => param.ParameterType == typeof(CancellationToken) && param.IsOptional))
                .Prettify()
                .ToList();

            violators.Write(nameof(OptionalTokenPolicy), visible);

            Assert.IsEmpty(violators);
        }

        [TestCase(true)]
        [TestCase(false)]
        public static void MandatoryTokenPolicy(bool visible)
        {
            var violators = mandatoryTokenPolicy
                .Where(method => method.IsVisible() == visible &&
                    !method.GetParameters().Any(param => param.ParameterType == typeof(CancellationToken) && !param.IsOptional))
                .Prettify()
                .ToList();

            violators.Write(nameof(MandatoryTokenPolicy), visible);

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void CancellationTokenNumberPolicy()
        {
            var violators = allMethods
                .Where(method => method.GetParameters().Count(param => param.ParameterType == typeof(CancellationToken)) > 1)
                .Prettify()
                .ToList();

            violators.Write(nameof(CancellationTokenNumberPolicy));

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void CancellationTokenPositionPolicy()
        {
            var candidates = allMethods
                .Where(method => !(typeof(Delegate).IsAssignableFrom(method.DeclaringType) && method.Name == "BeginInvoke"))
                .Where(method => method.GetParameters().Any(param => param.ParameterType == typeof(CancellationToken)))
                .ToList();

            var violators = candidates
                .Where(method => method.GetParameters().Last().ParameterType != typeof(CancellationToken))
                .Prettify()
                .ToList();

            violators.Write(nameof(CancellationTokenPositionPolicy));

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void CancellationTokenNamePolicy()
        {
            var violators = allMethods
                .Where(method => method.GetParameters().Any(param =>
                    param.ParameterType == typeof(CancellationToken) &&
                    !param.Name.Equals("cancellationToken", StringComparison.Ordinal) &&
                    !param.Name.EndsWith("CancellationToken", StringComparison.Ordinal))) // e.g. stopRequestedCancellationToken
                .Prettify()
                .ToList();

            violators.Write(nameof(CancellationTokenPositionPolicy));

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void FuncParameterMandatoryTokenAndPositionPolicy()
        {
            var violators = allMethods
                .Where(method => !typeof(NServiceBus.Pipeline.IBehavior).IsAssignableFrom(method.DeclaringType))
                .Where(method => method.GetParameters()
                    .Where(param => param.ParameterType != typeof(Delegate) && typeof(Delegate).IsAssignableFrom(param.ParameterType))
                    .Select(param => param.ParameterType.GetMethod("Invoke"))
                    .Where(invoke => typeof(Task).IsAssignableFrom(invoke.ReturnType))
                    .Select(invoke => invoke.GetParameters())
                    .Any(parameters => !parameters.Any() || !typeof(CancellationToken).IsAssignableFrom(parameters.Last().ParameterType)))
                .Prettify()
                .ToList();

            violators.Write(nameof(FuncParameterMandatoryTokenAndPositionPolicy));

            Assert.IsEmpty(violators);
        }

        static bool IsObsolete(this Type type, Stack<Type> seenTypes = null)
        {
            if (seenTypes == null)
            {
                seenTypes = new Stack<Type>();
            }

            if (seenTypes.Contains(type))
            {
                return false;
            }

            seenTypes.Push(type);
            try
            {
                return type.GetCustomAttributes<ObsoleteAttribute>(true).Any() ||
                    type.GetGenericArguments().Any(arg => arg.IsObsolete(seenTypes)) ||
                    (type.DeclaringType != null && type.DeclaringType.IsObsolete(seenTypes));
            }
            finally
            {
                _ = seenTypes.Pop();
            }
        }

        static bool IsCompilerGenerated(this MethodInfo method) =>
            method.GetCustomAttributes(typeof(CompilerGeneratedAttribute)).Any() ||
            method.DeclaringType.GetCustomAttributes(typeof(CompilerGeneratedAttribute)).Any();

        static bool IsObsolete(this MethodInfo method) =>
            method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any() ||
            method.ReturnType.IsObsolete() ||
            method.GetParameters().Any(param => param.ParameterType.IsObsolete());

        static bool IsOnOneOf(this MethodInfo method, params Type[] types) =>
            types.Any(type => method.IsOn(type));

        static bool IsOn(this MethodInfo method, Type type) =>
            type.IsAssignableFrom(method.DeclaringType) ||
            (method.GetCustomAttributes<ExtensionAttribute>().Any() && type.IsAssignableFrom(method.GetParameters().First().ParameterType));

        static bool IsVisible(this MethodInfo method) =>
            method.DeclaringType.IsVisible && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

        static bool HasCancellableContext(this MethodInfo method) =>
            method.GetParameters().Any(parameter => typeof(ICancellableContext).IsAssignableFrom(parameter.ParameterType));

        static IEnumerable<MethodPrinter> Prettify(this IEnumerable<MethodInfo> methods) =>
            methods
                .Sort()
                .Select(method => new MethodPrinter(method));

        static IOrderedEnumerable<MethodInfo> Sort(this IEnumerable<MethodInfo> methods) =>
            methods
                .OrderBy(method => method.DeclaringType.Namespace)
                .ThenBy(method => method.DeclaringType.Name)
                .ThenBy(method => method.Name)
                .ThenBy(method => method.ToString());

        static void Write<T>(this IEnumerable<T> violators, string testName, bool visible) =>
            Console.Error.WriteLine($"Violators of {testName} for {(visible ? "" : "in")}visible methods:{Environment.NewLine}{string.Join(Environment.NewLine, violators)}");

        static void Write<T>(this IEnumerable<T> violators, string testName) =>
            Console.Error.WriteLine($"Violators of {testName}:{Environment.NewLine}{string.Join(Environment.NewLine, violators)}");

        class MethodPrinter
        {
            readonly MethodInfo method;

            public MethodPrinter(MethodInfo method) => this.method = method;

            public override string ToString() => $"{method.DeclaringType.FullName} {{ {method} }}";
        }
    }
}

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
        static readonly string[] behaviorMethodNames = new[]
        {
            "Invoke",
            "Terminate",
            "Fork"
        };

        static readonly List<MethodInfo> methods = typeof(IMessage).Assembly.GetTypes()
            .Where(type => type != typeof(TaskEx)) // TODO: Consider removing this type?
            .Where(type => !type.IsObsolete())
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => !method.IsObsolete()))
            .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType))
            .Where(method => !method.IsCompilerGenerated())
            .ToList();

#pragma warning disable IDE0001 // Simplify Names

        static readonly List<MethodInfo> nonPublicMandatoryTokenExceptions = methods
            .Where(method =>
                // These implement timers
                method.IsOn(typeof(NServiceBus.IAsyncTimer)) ||
                method.IsOn(typeof(NServiceBus.DelayedMessagePoller)) ||
                // Context/Pipeline related
                method.IsOn(typeof(NServiceBus.PipelineInvocationExtensions)) ||
                method.IsOn(typeof(NServiceBus.IncomingMessageOperations)) ||
                (method.IsOn(typeof(NServiceBus.IUnicastPublishRouter)) && method.Name == "Route")
            )
            .ToList();

        static readonly List<MethodInfo> noTokenPolicy = methods
            .Where(method =>
                method.IsOnBehaviorOrContext() ||
                nonPublicMandatoryTokenExceptions.Contains(method) ||
                method.IsOn(typeof(NServiceBus.IHandleMessages<>)) ||
                method.IsOn(typeof(NServiceBus.IHandleTimeouts<>)) ||
                method.IsOn(typeof(NServiceBus.IMessageProcessingContext)) ||
                method.IsOn(typeof(NServiceBus.IPipelineContext)) ||
                (method.IsOn(typeof(NServiceBus.OnSatelliteMessage)) && method.Name == "EndInvoke") ||
                method.IsOn(typeof(NServiceBus.Saga)) ||
                method.IsOn(typeof(NServiceBus.MessageMutator.IMutateIncomingMessages)) ||
                method.IsOn(typeof(NServiceBus.MessageMutator.IMutateIncomingTransportMessages)) ||
                method.IsOn(typeof(NServiceBus.MessageMutator.IMutateOutgoingMessages)) ||
                method.IsOn(typeof(NServiceBus.MessageMutator.IMutateOutgoingTransportMessages)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.Behavior<>)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.ForkConnector<,>)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.IBehavior<,>)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.MessageHandler)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.PipelineTerminator<>)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.StageConnector<,>)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.StageForkConnector<,,>)) ||
                method.IsOn(typeof(NServiceBus.Sagas.IHandleSagaNotFound)) ||
                (method.IsOn(typeof(NServiceBus.Transport.OnError)) && method.Name == "EndInvoke") ||
                (method.IsOn(typeof(NServiceBus.Transport.OnMessage)) && method.Name == "EndInvoke"))
            .ToList();

        static readonly List<MethodInfo> optionalTokenPolicy = methods
            .Where(method =>
                method.IsOn(typeof(NServiceBus.Endpoint)) ||
                method.IsOn(typeof(NServiceBus.IEndpointInstance)) ||
                method.IsOn(typeof(NServiceBus.IMessageSession)) ||
                method.IsOn(typeof(NServiceBus.IStartableEndpoint)) ||
                method.IsOn(typeof(NServiceBus.IStartableEndpointWithExternallyManagedContainer)))
            .ToList();

        static readonly List<MethodInfo> mandatoryTokenPolicy = methods
            .Where(method => !nonPublicMandatoryTokenExceptions.Contains(method))
            .Where(method =>
                (!method.IsVisible() && !method.IsOnBehaviorOrContext() && !optionalTokenPolicy.Contains(method)) ||
                method.IsOn(typeof(NServiceBus.LearningTransport)) ||
                (method.IsOn(typeof(NServiceBus.OnSatelliteMessage)) && method.Name == "Invoke") ||
                method.IsOn(typeof(NServiceBus.DataBus.IDataBus)) ||
                method.IsOn(typeof(NServiceBus.Features.FeatureStartupTask)) ||
                method.IsOn(typeof(NServiceBus.Installation.INeedToInstallSomething)) ||
                method.IsOn(typeof(NServiceBus.Outbox.IOutboxStorage)) ||
                method.IsOn(typeof(NServiceBus.Outbox.OutboxTransaction)) ||
                method.IsOn(typeof(NServiceBus.Persistence.CompletableSynchronizedStorageSession)) ||
                method.IsOn(typeof(NServiceBus.Persistence.ISynchronizedStorage)) ||
                method.IsOn(typeof(NServiceBus.Persistence.ISynchronizedStorageAdapter)) ||
                method.IsOn(typeof(NServiceBus.Sagas.IFindSagas<>.Using<>)) ||
                method.IsOn(typeof(NServiceBus.Sagas.ISagaPersister)) ||
                method.IsOn(typeof(NServiceBus.Transport.IMessageDispatcher)) ||
                method.IsOn(typeof(NServiceBus.Transport.IMessageReceiver)) ||
                method.IsOn(typeof(NServiceBus.Transport.ISubscriptionManager)) ||
                (method.IsOn(typeof(NServiceBus.Transport.OnError)) && method.Name == "Invoke") ||
                (method.IsOn(typeof(NServiceBus.Transport.OnMessage)) && method.Name == "Invoke") ||
                method.IsOn(typeof(NServiceBus.Transport.TransportDefinition)) ||
                method.IsOn(typeof(NServiceBus.Transport.TransportInfrastructure)) ||
                method.IsOn(typeof(NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions.ISubscriptionStorage)))
            .ToList();
#pragma warning restore IDE0001 // Simplify Names

        [TestCase(true)]
        [TestCase(false)]
        public static void AllMethodsHaveATokenPolicy(bool visible)
        {
            var violators = methods
                .Where(method => method.IsVisible() == visible)
                .Except(noTokenPolicy)
                .Except(optionalTokenPolicy)
                .Except(mandatoryTokenPolicy)
                .Prettify()
                .ToList();

            violators.Write(nameof(AllMethodsHaveATokenPolicy), visible);

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void NoOverlaps()
        {
            var nonePlusOptional = noTokenPolicy.Intersect(optionalTokenPolicy).Prettify().ToList();
            var optionalPlusMandatory = optionalTokenPolicy.Intersect(mandatoryTokenPolicy).Prettify().ToList();
            var mandatoryPlusNone = mandatoryTokenPolicy.Intersect(noTokenPolicy).Prettify().ToList();

            nonePlusOptional.Write("Methods in NoPolicy + OptionalPolicy");
            Console.Error.WriteLine();
            optionalPlusMandatory.Write("Methods in OptionalPolicy + MandatoryPolicy");
            Console.Error.WriteLine();
            mandatoryPlusNone.Write("Methods in MandatoryPolicy + NoPolicy");

            Assert.IsEmpty(nonePlusOptional);
            Assert.IsEmpty(optionalPlusMandatory);
            Assert.IsEmpty(mandatoryPlusNone);
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
            var violators = methods
                .Where(method => method.GetParameters().Count(param => param.ParameterType == typeof(CancellationToken)) > 1)
                .Prettify()
                .ToList();

            violators.Write(nameof(CancellationTokenNumberPolicy));

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void CancellationTokenPositionPolicy()
        {
            var candidates = methods
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
            // CancellationToken names must be either 'cancellationToken' or end with 'CancellationToken' (i.e. 'stopRequestedCancellationToken')
            var violators = methods
                .Where(method => !method.IsCompilerGenerated()) // Don't care about in-method closures
                .Where(method => method.GetParameters().Any(param => param.ParameterType == typeof(CancellationToken) && !(param.Name == "cancellationToken" || param.Name.EndsWith("CancellationToken"))))
                .Prettify()
                .ToList();

            violators.Write(nameof(CancellationTokenPositionPolicy));

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void FuncParameterMandatoryTokenAndPositionPolicy()
        {
            var violators = methods
                .Where(method => !typeof(NServiceBus.Pipeline.IBehavior).IsAssignableFrom(method.DeclaringType))
                .Where(method => method.GetParameters()
                    .Where(param => typeof(Delegate).IsAssignableFrom(param.ParameterType))
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
                seenTypes.Pop();
            }
        }

        static bool IsObsolete(this MethodInfo method) =>
            method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any() ||
            method.ReturnType.IsObsolete() ||
            method.GetParameters().Any(param => param.ParameterType.IsObsolete());

        static bool IsOn(this MethodInfo method, Type type) =>
            method.DeclaringType == type || type.IsAssignableFrom(method.DeclaringType) || (method.GetCustomAttributes<ExtensionAttribute>().Any() && type.IsAssignableFrom(method.GetParameters().First().ParameterType));

        static bool IsVisible(this MethodInfo method) =>
            method.DeclaringType.IsVisible && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

        static bool IsOnBehaviorOrContext(this MethodInfo method)
        {
            if (typeof(NServiceBus.Pipeline.IBehavior).IsAssignableFrom(method.DeclaringType) || typeof(IPipeline).IsAssignableFrom(method.DeclaringType))
            {
                if (behaviorMethodNames.Contains(method.Name))
                {
                    return true;
                }

                return method.GetParameters().Any(parameter => typeof(ICancellableContext).IsAssignableFrom(parameter.ParameterType));
            }

            if (typeof(ICancellableContext).IsAssignableFrom(method.DeclaringType))
            {
                return true;
            }

            if (method.IsStatic && method.DeclaringType.IsSealed && typeof(NServiceBus.Pipeline.IBehavior).IsAssignableFrom(method.GetParameters().First().ParameterType))
            {
                return true;
            }

            return false;
        }

        static bool IsCompilerGenerated(this MethodInfo method) =>
            method.GetCustomAttributes(typeof(CompilerGeneratedAttribute)).Any() ||
            method.DeclaringType.GetCustomAttributes(typeof(CompilerGeneratedAttribute)).Any();

        static IEnumerable<MethodPrinter> Prettify(this IEnumerable<MethodInfo> methods) =>
            methods
                .OrderBy(method => method.DeclaringType.Namespace)
                .ThenBy(method => method.DeclaringType.Name)
                .ThenBy(method => method.Name)
                .ThenBy(method => method.ToString())
                .Select(method => new MethodPrinter(method));

        static void Write(this IEnumerable<MethodPrinter> violators, string testName, bool visible) =>
            Console.Error.WriteLine($"Violators of {testName} for {(visible ? "" : "in")}visible methods:{Environment.NewLine}{string.Join(Environment.NewLine, violators)}");

        static void Write(this IEnumerable<MethodPrinter> violators, string testName) =>
            Console.Error.WriteLine($"Violators of {testName}:{Environment.NewLine}{string.Join(Environment.NewLine, violators)}");

        class MethodPrinter
        {
            readonly MethodInfo method;

            public MethodPrinter(MethodInfo method) => this.method = method;

            public override string ToString() => $"{method.DeclaringType.FullName} {{ {method} }}";
        }
    }
}

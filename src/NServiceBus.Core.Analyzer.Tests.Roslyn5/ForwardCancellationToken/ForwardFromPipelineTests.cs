#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MessageMutator;
using Pipeline;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class ForwardFromPipelineTests : AnalyzerTestFixture<ForwardCancellationTokenAnalyzer>
{
    [Test]
    public void EachTypeHasABasicTest()
    {
        // These abstract classes are only extended internally
        var ignoredTypes = new[] {
#pragma warning disable IDE0001 // Simplify Names
            typeof(NServiceBus.Pipeline.ForkConnector<,>),
            typeof(NServiceBus.Pipeline.PipelineTerminator<>),
            typeof(NServiceBus.Pipeline.StageConnector<,>),
            typeof(NServiceBus.Pipeline.StageForkConnector<,,>),
#pragma warning restore IDE0001 // Simplify Names
        };

        var pipelineTypes = typeof(EndpointConfiguration).Assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsSealed && !t.GetCustomAttributes(true).OfType<ObsoleteAttribute>().Any())
            .Where(t => t.IsInterface || t.IsAbstract)
            .Where(t => !ignoredTypes.Contains(t))
            .Where(HasTaskReturningMethodWithContextParameter)
            .OrderBy(t => t.FullName)
            .ToArray();

        var typesCoveredByThisTest = GetType().GetMethod(nameof(RunTestOnType))
            .GetCustomAttributes(typeof(TestCaseAttribute), true)
            .OfType<TestCaseAttribute>()
            .Select(att => att.Arguments.First() as Type)
            .OrderBy(t => t.FullName)
            .ToArray();

        var coveredTypes = pipelineTypes.Intersect(typesCoveredByThisTest).ToArray();
        var missingTestCases = pipelineTypes.Except(typesCoveredByThisTest).ToArray();

        TestContext.Out.WriteLine($"Types covered by a {nameof(RunTestOnType)} TestCase to ensure analyzer support:");
        foreach (var t in coveredTypes)
        {
            TestContext.Out.WriteLine(t.FullName);
        }

        TestContext.Out.WriteLine();
        TestContext.Out.WriteLine($"Types missing a {nameof(RunTestOnType)} TestCase:");
        foreach (var t in missingTestCases)
        {
            TestContext.Out.WriteLine(t.FullName);
        }

        NUnit.Framework.Assert.That(missingTestCases.Length, Is.EqualTo(0), $"One or more pipeline type(s) are not covered by the {nameof(RunTestOnType)} test in this class.");
    }

    [TestCase(typeof(IHandleMessages<>), "TestMessage", "Handle", "TestMessage message, IMessageHandlerContext context")]
    [TestCase(typeof(IAmStartedByMessages<>), "TestMessage", "Handle", "TestMessage message, IMessageHandlerContext context")]
    [TestCase(typeof(IHandleTimeouts<>), "TestTimeout", "Timeout", "TestTimeout state, IMessageHandlerContext context")]
    [TestCase(typeof(NServiceBus.ISagaNotFoundHandler), null, "Handle", "object message, IMessageProcessingContext context")]
    [TestCase(typeof(Behavior<>), "IIncomingLogicalMessageContext", "Invoke", "IIncomingLogicalMessageContext context, Func<Task> next")]
    [TestCase(typeof(IBehavior<,>), "IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext", "Invoke", "IIncomingPhysicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next")]
    [TestCase(typeof(IMutateIncomingTransportMessages), null, "MutateIncoming", "MutateIncomingTransportMessageContext context")]
    [TestCase(typeof(IMutateIncomingMessages), null, "MutateIncoming", "MutateIncomingMessageContext context")]
    [TestCase(typeof(IMutateOutgoingTransportMessages), null, "MutateOutgoing", "MutateOutgoingTransportMessageContext context")]
    [TestCase(typeof(IMutateOutgoingMessages), null, "MutateOutgoing", "MutateOutgoingMessageContext context")]
    public Task RunTestOnType(Type type, string genericTypeArgs, string methodName, string methodArguments)
    {
        var baseTypeWithArgs = Regex.Replace(type.Name, "`.*", "") + (genericTypeArgs != null ? $"<{genericTypeArgs}>" : "");
        var methodPreamble = !type.IsInterface && type.IsAbstract
            ? "public override Task"
            : "public Task";

        var code = $$"""
            using NServiceBus;
            using NServiceBus.MessageMutator;
            using NServiceBus.Pipeline;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            public class Foo : {{baseTypeWithArgs}}
            {
                {{methodPreamble}} {{methodName}}({{methodArguments}})
                {
                    return [|TestMethod()|];
                }

                static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
            }
            public class TestMessage : ICommand {}
            public class TestTimeout {}
            """;

        return Assert(code, DiagnosticIds.ForwardCancellationToken);
    }

    static bool HasTaskReturningMethodWithContextParameter(Type type)
    {
        var typeList = type.GetInterfaces().ToList();
        typeList.Add(type);

        foreach (var typeOrInterface in typeList.Distinct())
        {
            foreach (var method in typeOrInterface.GetMethods())
            {
                var isAwaitable = method.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;

                if (!isAwaitable)
                {
                    continue;
                }

                foreach (var param in method.GetParameters())
                {
                    if (typeof(ICancellableContext).IsAssignableFrom(param.ParameterType))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
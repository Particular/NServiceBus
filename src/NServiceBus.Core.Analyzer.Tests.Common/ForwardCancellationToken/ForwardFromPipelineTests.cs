namespace NServiceBus.Core.Analyzer.Tests
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Helpers;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline;
    using NServiceBus.Sagas;
    using NUnit.Framework;

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

            TestContext.WriteLine($"Types covered by a {nameof(RunTestOnType)} TestCase to ensure analyzer support:");
            foreach (var t in coveredTypes)
            {
                TestContext.WriteLine(t.FullName);
            }

            TestContext.WriteLine();
            TestContext.WriteLine($"Types missing a {nameof(RunTestOnType)} TestCase:");
            foreach (var t in missingTestCases)
            {
                TestContext.WriteLine(t.FullName);
            }

            NUnit.Framework.Assert.AreEqual(0, missingTestCases.Length, $"One or more pipeline type(s) are not covered by the {nameof(RunTestOnType)} test in this class.");
        }

        [TestCase(typeof(IHandleMessages<>), "TestMessage", "Handle", "TestMessage message, IMessageHandlerContext context")]
        [TestCase(typeof(IAmStartedByMessages<>), "TestMessage", "Handle", "TestMessage message, IMessageHandlerContext context")]
        [TestCase(typeof(IHandleTimeouts<>), "TestTimeout", "Timeout", "TestTimeout state, IMessageHandlerContext context")]
        [TestCase(typeof(IHandleSagaNotFound), null, "Handle", "object message, IMessageProcessingContext context")]
        [TestCase(typeof(Behavior<>), "IIncomingLogicalMessageContext", "Invoke", "IIncomingLogicalMessageContext context, Func<Task> next")]
        [TestCase(typeof(IBehavior<,>), "IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext", "Invoke", "IIncomingPhysicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next")]
        [TestCase(typeof(IMutateIncomingTransportMessages), null, "MutateIncoming", "MutateIncomingTransportMessageContext context")]
        [TestCase(typeof(IMutateIncomingMessages), null, "MutateIncoming", "MutateIncomingMessageContext context")]
        [TestCase(typeof(IMutateOutgoingTransportMessages), null, "MutateOutgoing", "MutateOutgoingTransportMessageContext context")]
        [TestCase(typeof(IMutateOutgoingMessages), null, "MutateOutgoing", "MutateOutgoingMessageContext context")]
        public Task RunTestOnType(Type type, string genericTypeArgs, string methodName, string methodArguments)
        {
            const string codeFormat =
@"using NServiceBus;
using NServiceBus.MessageMutator;
using NServiceBus.Pipeline;
using NServiceBus.Sagas;
using System;
using System.Threading;
using System.Threading.Tasks;
public class Foo : BASE_TYPE_WITH_ARGUMENTS
{
    public Task METHOD_NAME(METHOD_ARGUMENTS)
    {
        return [|TestMethod()|];
    }

    static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}
public class TestTimeout {}";

            var baseTypeWithArgs = Regex.Replace(type.Name, "`.*", "") + (genericTypeArgs != null ? $"<{genericTypeArgs}>" : "");

            // Too confusing to do normal string interpolation or formatting with all the curly braces
            var code = codeFormat
                .Replace("BASE_TYPE_WITH_ARGUMENTS", baseTypeWithArgs)
                .Replace("METHOD_NAME", methodName)
                .Replace("METHOD_ARGUMENTS", methodArguments);

            if (!type.IsInterface && type.IsAbstract)
            {
                code = code.Replace("public Task", "public override Task");
            }

            return Assert(ForwardCancellationTokenAnalyzer.DiagnosticId, code);
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

    public class ForwardFromPipelineTestsCSharp8 : ForwardFromPipelineTests
    {
        protected override LanguageVersion AnalyzerLanguageVersion => LanguageVersion.CSharp8;
    }

    public class ForwardFromPipelineTestsCSharp9 : ForwardFromPipelineTestsCSharp8
    {
        protected override LanguageVersion AnalyzerLanguageVersion => LanguageVersion.CSharp9;
    }

#if ROSLYN4
    public class ForwardFromPipelineTestsCSharp10 : ForwardFromPipelineTestsCSharp9
    {
        protected override LanguageVersion AnalyzerLanguageVersion => LanguageVersion.CSharp10;
    }
#endif
}

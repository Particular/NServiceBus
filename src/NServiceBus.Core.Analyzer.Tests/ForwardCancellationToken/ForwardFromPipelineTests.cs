#pragma warning disable IDE0022 // Use expression body for methods
namespace NServiceBus.Core.Analyzer.Tests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Helpers;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NServiceBus.Pipeline;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class ForwardFromPipelineTests : DiagnosticVerifier
    {
        [Test]
        [SuppressMessage("Style", "IDE0001:Simplify Names", Justification = "Clarity of different type names")]
        public void EachTypeHasABasicTest()
        {
            var ignoredTypes = new[] {
                typeof(NServiceBus.Pipeline.ForkConnector<,>),
                typeof(NServiceBus.Pipeline.PipelineTerminator<>),
                typeof(NServiceBus.Pipeline.StageConnector<,>),
                typeof(NServiceBus.Pipeline.StageForkConnector<,,>),
            };

            var pipelineTypes = typeof(EndpointConfiguration).Assembly.GetTypes()
                .Where(t => t.IsPublic && !t.IsSealed && !t.GetCustomAttributes(true).OfType<ObsoleteAttribute>().Any())
                .Where(t => t.IsInterface || t.IsAbstract)
                .Where(t => !ignoredTypes.Contains(t))
                .Where(HasMethodWithContextParameter)
                .OrderBy(t => t.FullName)
                .ToArray();

            var typesCoveredByThisTest = GetType().GetMethod(nameof(RunTestOnType))
                .GetCustomAttributes(typeof(TestCaseAttribute), true)
                .OfType<TestCaseAttribute>()
                .Select(att => att.Arguments.First() as Type)
                .OrderBy(t => t.FullName)
                .ToArray();

            TestContext.WriteLine("Types that should have analyzer support:");
            foreach (var t in pipelineTypes)
            {
                TestContext.WriteLine(t.FullName);
            }

            TestContext.WriteLine();
            TestContext.WriteLine($"Types covered by {nameof(RunTestOnType)}:");
            foreach (var t in typesCoveredByThisTest)
            {
                TestContext.WriteLine(t.FullName);
            }

            Assert.True(pipelineTypes.Intersect(typesCoveredByThisTest).Count() == pipelineTypes.Length,
                $"One or more pipeline type(s) are not covered by the {nameof(RunTestOnType)} test in this class.");
        }

        [TestCase(typeof(IHandleMessages<>), "TestMessage", "Handle", "TestMessage message, IMessageHandlerContext context")]
        [TestCase(typeof(IAmStartedByMessages<>), "TestMessage", "Handle", "TestMessage message, IMessageHandlerContext context")]
        [TestCase(typeof(IHandleTimeouts<>), "TestTimeout", "Timeout", "TestTimeout state, IMessageHandlerContext context")]
        [TestCase(typeof(IHandleSagaNotFound), null, "Handle", "object message, IMessageProcessingContext context")]
        [TestCase(typeof(Behavior<>), "IIncomingLogicalMessageContext", "Invoke", "IIncomingLogicalMessageContext context, Func<Task> next")]
        [TestCase(typeof(IBehavior<,>), "IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext", "Invoke", "IIncomingPhysicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next")]
        public Task RunTestOnType(Type type, string genericTypeArgs, string methodName, string methodArguments)
        {
            const string sourceFormat =
        @"
using NServiceBus;
using NServiceBus.Pipeline;
using NServiceBus.Sagas;
using System;
using System.Threading;
using System.Threading.Tasks;
public class Foo : BASE_TYPE_WITH_ARGUMENTS
{
    public Task METHOD_NAME(METHOD_ARGUMENTS)
    {
        return TestMethod();
    }

    static Task TestMethod(CancellationToken token = default(CancellationToken)) { return Task.CompletedTask; }
}
public class TestMessage : ICommand {}
public class TestTimeout {}
";

            var baseTypeWithArgs = Regex.Replace(type.Name, "`.*", "") + (genericTypeArgs != null ? $"<{genericTypeArgs}>" : "");

            // Too confusing to do normal string interpolation or formatting with all the curly braces
            var code = sourceFormat
                .Replace("BASE_TYPE_WITH_ARGUMENTS", baseTypeWithArgs)
                .Replace("METHOD_NAME", methodName)
                .Replace("METHOD_ARGUMENTS", methodArguments);

            if (!type.IsInterface && type.IsAbstract)
            {
                code = code.Replace("public Task", "public override Task");
            }

            var expected = NotForwardedAt(12, 16);

            TestContext.WriteLine($"Source Code for test case: {type.FullName}:");
            TestContext.WriteLine(code);

            return Verify(code, expected);
        }

        static bool HasMethodWithContextParameter(Type type)
        {
            var typeList = type.GetInterfaces().ToList();
            typeList.Add(type);

            if (type == typeof(IAmStartedByMessages<>))
            {

            }

            foreach (var typeOrInterface in typeList.Distinct())
            {
                foreach (var method in typeOrInterface.GetMethods())
                {
                    foreach (var p in method.GetParameters())
                    {
                        if (typeof(IPipelineContext).IsAssignableFrom(p.ParameterType) || typeof(IBehaviorContext).IsAssignableFrom(p.ParameterType))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        static DiagnosticResult NotForwardedAt(int line, int character)
        {
            return new DiagnosticResult
            {
                Id = "NSB0002",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, character) }
            };
        }

        protected override DiagnosticAnalyzer GetAnalyzer() => new ForwardCancellationTokenAnalyzer();
    }
}
#pragma warning restore IDE0022 // Use expression body for methods
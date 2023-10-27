﻿namespace NServiceBus.Testing.Tests.Fakes
{
    using System;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using Pipeline;

    [TestFixture]
    public class TestableContextChecker
    {
        [Test]
        public void ShouldProvideTestableImplementationForAllBehaviorContexts()
        {
            var nservicebusAssembly = Assembly.GetAssembly(typeof(IMessageHandlerContext));
            var testingAssembly = Assembly.GetAssembly(typeof(TestableMessageSession));
            var behaviorContextType = typeof(IBehaviorContext);

            var behaviorContextInterfaces = nservicebusAssembly.DefinedTypes
                .Where(x => x.IsInterface && behaviorContextType.IsAssignableFrom(x))
                .Where(x => !x.GetCustomAttributes().Any(att => att.GetType() == typeof(ObsoleteAttribute)))
                .Except(new[]
                {
                    typeof(PipelineTerminator<>.ITerminatingContext),
                    typeof(IRecoverabilityActionContext),
                    typeof(IAuditActionContext)
                });

            foreach (var behaviorContextInterface in behaviorContextInterfaces)
            {
                var testableImplementationName = string.Concat("Testable", behaviorContextInterface.Name.AsSpan(1));

                if (!testingAssembly.DefinedTypes.Any(t => t.Name == testableImplementationName && behaviorContextInterface.IsAssignableFrom(t)))
                {
                    Assert.Fail($"Found no testable implementation for {behaviorContextInterface.FullName}. Expecting an implementation named {testableImplementationName}.");
                }
            }
        }
    }
}
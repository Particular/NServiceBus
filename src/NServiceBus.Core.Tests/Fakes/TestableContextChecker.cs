namespace NServiceBus.Testing.Tests.Fakes
{
    using System.Linq;
    using System.Reflection;
    using Pipeline;
    using NUnit.Framework;

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
                .Except(new[]
                {
                    typeof(PipelineTerminator<>.ITerminatingContext)
                });

            foreach (var behaviorContextInterface in behaviorContextInterfaces)
            {
                var testableImplementationName = "Testable" + behaviorContextInterface.Name.Substring(1);

                if (!testingAssembly.DefinedTypes.Any(t => t.Name == testableImplementationName && behaviorContextInterface.IsAssignableFrom(t)))
                {
                    Assert.Fail($"Found no testable implementation for {behaviorContextInterface.FullName}. Expecting an implementation named {testableImplementationName}.");
                }
            }
        }
    }
}
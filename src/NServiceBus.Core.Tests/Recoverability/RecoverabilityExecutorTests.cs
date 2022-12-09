namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;
    using Extensibility;
    using NServiceBus.Pipeline;
    using Transport;
    using NUnit.Framework;

    [TestFixture]
    public class RecoverabilityExecutorTests
    {
        [Test]
        public async Task Should_use_error_context_extensions_as_extensions_root()
        {
            var newValue = Guid.NewGuid();
            var existingValue = Guid.NewGuid();

            ContextBag pipelineExtensions = null;
            var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>
            {
                OnInvoke = context =>
                {
                    context.Extensions.SetOnRoot("new value", newValue);
                    pipelineExtensions = context.Extensions;
                }
            };

            var executor = CreateRecoverabilityExecutor(recoverabilityPipeline);

            ErrorContext errorContext = CreateErrorContext();
            errorContext.Extensions.Set("existing value", existingValue);

            await executor.Invoke(errorContext);

            Assert.AreEqual(newValue, errorContext.Extensions.Get<Guid>("new value"));
            Assert.AreEqual(existingValue, pipelineExtensions.Get<Guid>("existing value"));
        }

        static RecoverabilityPipelineExecutor<object> CreateRecoverabilityExecutor(TestableMessageOperations.Pipeline<IRecoverabilityContext> recoverabilityPipeline)
        {
            var executor = new RecoverabilityPipelineExecutor<object>(
                new ServiceCollection().BuildServiceProvider(),
                null,
                new TestableMessageOperations(),
                null, (_, _) => RecoverabilityAction.Discard("test"),
                recoverabilityPipeline,
                new FaultMetadataExtractor(new Dictionary<string, string>(0), _ => { }),
                null);
            return executor;
        }

        static ErrorContext CreateErrorContext() => new(new Exception("test"), new Dictionary<string, string>(), Guid.NewGuid().ToString(), Array.Empty<byte>(), new TransportTransaction(), 10, "receive address", new ContextBag());
    }
}
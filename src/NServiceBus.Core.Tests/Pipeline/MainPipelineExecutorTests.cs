namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using OpenTelemetry.Helpers;
    using Transport;

    [TestFixture]
    public class MainPipelineExecutorTests
    {
        TestingActivityListener nsbActivityListener;

        [OneTimeSetUp]
        public void Setup()
        {
            nsbActivityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            nsbActivityListener.Dispose();
        }

        [Test]
        public async Task When_invoking_pipeline()
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);

            await executor.Invoke(CreateMessageContext());

            Assert.NotNull(receivePipeline.PipelineAcitivty);
            Assert.AreEqual(ActivityNames.IncomingMessageActivityName, receivePipeline.PipelineAcitivty.OperationName);
            Assert.AreEqual("process message", receivePipeline.PipelineAcitivty.DisplayName);
        }

        [Test]
        public async Task When_pipeline_successful()
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);

            await executor.Invoke(CreateMessageContext());

            Assert.AreEqual(ActivityStatusCode.Ok, receivePipeline.PipelineAcitivty.Status);
        }

        [Test]
        public void When_pipeline_throws_exception()
        {
            var executor = CreateMainPipelineExecutor(out var receivePipeline);
            receivePipeline.ThrowsException = true;

            Assert.ThrowsAsync<Exception>(async () => await executor.Invoke(CreateMessageContext()));

            Assert.AreEqual(ActivityStatusCode.Error, receivePipeline.PipelineAcitivty.Status);
        }

        static MessageContext CreateMessageContext(Dictionary<string, string> messageHeaders = null, ContextBag contextBag = null)
        {
            return new MessageContext(
                Guid.NewGuid().ToString(),
                messageHeaders ?? new Dictionary<string, string>(),
                Array.Empty<byte>(),
                new TransportTransaction(),
                "receiver",
                contextBag ?? new ContextBag());
        }

        static MainPipelineExecutor CreateMainPipelineExecutor(out ReceivePipeline receivePipeline)
        {
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            receivePipeline = new ReceivePipeline();
            var executor = new MainPipelineExecutor(
                serviceProvider,
                new PipelineCache(serviceProvider, new PipelineModifications()),
                new MessageOperations(null, null, null, null, null, null, null),
                new Notification<ReceivePipelineCompleted>(),
                receivePipeline,
                new ActivityFactory());

            return executor;
        }

        class ReceivePipeline : IPipeline<ITransportReceiveContext>
        {
            public Activity PipelineAcitivty { get; set; }

            public bool ThrowsException { get; set; }

            public Task Invoke(ITransportReceiveContext context)
            {
                PipelineAcitivty = Activity.Current;

                if (ThrowsException)
                {
                    throw new Exception("Pipeline execution exception");
                }

                return Task.CompletedTask;
            }
        }
    }
}
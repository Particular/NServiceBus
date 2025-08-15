namespace NServiceBus;

using System.Threading;
using Core.Tests.Pipeline;

sealed class FakeRootContext() : PipelineRootContext(new ThrowingServiceProvider(), new TestableMessageOperations(),
    new ThrowingPipelineCache(), CancellationToken.None);
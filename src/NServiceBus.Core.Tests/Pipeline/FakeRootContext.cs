namespace NServiceBus;

using System;
using System.Threading;
using Core.Tests.Pipeline;

sealed class FakeRootContext(IServiceProvider serviceProvider = null) : PipelineRootContext(serviceProvider ?? new ThrowingServiceProvider(), new TestableMessageOperations(),
    new ThrowingPipelineCache(), CancellationToken.None);

namespace NServiceBus;

using System.Threading;
using Core.Tests.Pipeline;
using Transport;

sealed class FakeRootContext(HeaderPool headerPool) : PipelineRootContext(new ThrowingServiceProvider(), new TestableMessageOperations(),
    new ThrowingPipelineCache(), headerPool, CancellationToken.None)
{
    public FakeRootContext() : this(HeaderPool.AlwaysAllocate)
    {
    }
}
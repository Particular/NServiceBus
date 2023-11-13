namespace NServiceBus;

using System.Threading;

class FakeRootContext : PipelineRootContext
{
    public FakeRootContext() : base(null, null, null, CancellationToken.None)
    {
    }
}
namespace NServiceBus
{
    using System.Threading;

    class FakeRootContext : RootContext
    {
        public FakeRootContext() : base(null, null, null, CancellationToken.None)
        {
        }
    }
}
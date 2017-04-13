namespace NServiceBus.AcceptanceTesting.Support
{
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class ComponentRunner
    {
        public abstract string Name { get; }

        public virtual Task Start(CancellationToken token) => Task.FromResult(0);

        public virtual Task ComponentsStarted(CancellationToken token) => Task.FromResult(0);

        public virtual Task Stop() => Task.FromResult(0);
    }
}
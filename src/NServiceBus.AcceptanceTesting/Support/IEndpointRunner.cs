namespace NServiceBus.AcceptanceTesting.Support
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IEndpointRunner
    {
        bool FailOnErrorMessage { get; }
        Task Start(CancellationToken token);
        Task Whens(CancellationToken token);
        Task Stop();
        string Name();
    }
}
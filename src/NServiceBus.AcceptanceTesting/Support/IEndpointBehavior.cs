namespace NServiceBus.AcceptanceTesting.Support
{
    using System.Threading.Tasks;

    public interface IEndpointBehavior
    {
        Task<IEndpointRunner> CreateRunner(RunDescriptor run);
    }
}
namespace NServiceBus.AcceptanceTesting.Support
{
    using System.Threading.Tasks;

    public interface IComponentBehavior
    {
        Task<ComponentRunner> CreateRunner(RunDescriptor run);
    }
}
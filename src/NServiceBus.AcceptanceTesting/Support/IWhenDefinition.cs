namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading.Tasks;

    public interface IWhenDefinition
    {
        Task<bool> ExecuteAction(ScenarioContext context, IBusContext bus);

        Guid Id { get; }
    }
}
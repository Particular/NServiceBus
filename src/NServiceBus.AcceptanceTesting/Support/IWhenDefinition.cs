namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Threading.Tasks;

public interface IWhenDefinition
{
    Task<bool> ExecuteAction(ScenarioContext context, IMessageSession session, IServiceProvider serviceProvider);

    Guid Id { get; }
}
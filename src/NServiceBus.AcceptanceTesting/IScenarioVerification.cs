namespace NServiceBus.AcceptanceTesting
{
    using System;

    public interface IScenarioVerification
    {
        Type ContextType { get; set; }
        void Verify(ScenarioContext context);
    }
}
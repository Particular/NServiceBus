namespace NServiceBus.AcceptanceTesting
{
    using System;

    public class ScenarioVerification<T> : IScenarioVerification where T : ScenarioContext
    {
        public Action<T> Should { get; set; }
        public Type ContextType { get; set; }

        public void Verify(ScenarioContext context)
        {
            Should((T)context);
        }
    }
}
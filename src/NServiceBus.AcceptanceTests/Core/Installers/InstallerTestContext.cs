namespace NServiceBus.AcceptanceTests.Core.Installers;

using AcceptanceTesting;

class InstallerTestContext : ScenarioContext
{
    public bool InstallerCalled { get; set; }
    public bool FeatureSetupCalled { get; set; }

    public void MaybeCompleted() => MarkAsCompleted(InstallerCalled, FeatureSetupCalled);
}
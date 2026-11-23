namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using Features;
using Installation;
using Microsoft.Extensions.DependencyInjection;

class RecordingInstallerContext : ScenarioContext
{
    public bool InstallerCalled { get; set; }
}

class RecordingInstallerFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Services.AddSingleton(context.Settings.Get<RecordingInstallerContext>());
        context.AddInstaller<RecordingInstaller>();
    }

    class RecordingInstaller(RecordingInstallerContext testContext) : INeedToInstallSomething
    {
        public Task Install(string identity, CancellationToken cancellationToken = default)
        {
            testContext.InstallerCalled = true;
            testContext.MarkAsCompleted();
            return Task.CompletedTask;
        }
    }
}
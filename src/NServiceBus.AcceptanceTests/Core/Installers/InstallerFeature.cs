namespace NServiceBus.AcceptanceTests.Core.Installers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Features;
using Installation;

class InstallerFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.AddInstaller<InstallerFeatureContextInstaller>();

        var testContext = context.Settings.Get<InstallerTestContext>();
        testContext.FeatureSetupCalled = true;

        context.RegisterStartupTask<InstallerFeatureStartupTask>();
    }
}

class InstallerFeatureContextInstaller(InstallerTestContext testContext) : INeedToInstallSomething
{
    public Task Install(string identity, CancellationToken cancellationToken = default)
    {
        testContext.InstallerCalled = true;
        testContext.MaybeCompleted();
        return Task.CompletedTask;
    }
}

class InstallerFeatureStartupTask(InstallerTestContext testContext) : FeatureStartupTask
{
    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
    {
        testContext.MarkAsFailed(new InvalidOperationException("FeatureStartupTask should not be called"));
        return Task.CompletedTask;
    }

    protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
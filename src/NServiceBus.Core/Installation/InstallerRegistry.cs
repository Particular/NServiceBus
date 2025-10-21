#nullable enable
namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Installation;
using Microsoft.Extensions.DependencyInjection;

class InstallerRegistry
{
    public void SetUserName(string username) => installationUserName = username;

    public void Add<TInstaller>() where TInstaller : class, INeedToInstallSomething => installers.Add(typeof(TInstaller));

    public void AddScannedInstallers(IEnumerable<Type> scannedTypes)
    {
        foreach (var installerType in scannedTypes.Where(IsINeedToInstallSomething))
        {
            installers.Add(installerType);
        }
    }

    public async Task RunInstallers(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        foreach (var installerType in installers)
        {
            var installer = (INeedToInstallSomething)ActivatorUtilities.CreateInstance(serviceProvider, installerType);
            await installer.Install(installationUserName, cancellationToken).ConfigureAwait(false);
        }
    }

    string installationUserName = Environment.OSVersion.Platform == PlatformID.Win32NT ? $"{Environment.UserDomainName}\\{Environment.UserName}" : Environment.UserName;
    readonly HashSet<Type> installers = [];

    static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);
}
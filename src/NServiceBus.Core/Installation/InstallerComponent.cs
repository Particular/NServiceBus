#nullable enable
namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Installation;
using Microsoft.Extensions.DependencyInjection;

class InstallerComponent(InstallerComponent.Settings settings)
{
    public class Settings
    {
        public string InstallationUserName { get; set; } = Environment.OSVersion.Platform == PlatformID.Win32NT ? $"{Environment.UserDomainName}\\{Environment.UserName}" : Environment.UserName;

        public void Add<TInstaller>() where TInstaller : class, INeedToInstallSomething => installers.Add(typeof(TInstaller));

        public void AddScannedInstallers(IEnumerable<Type> scannedTypes)
        {
            foreach (var installerType in scannedTypes.Where(IsINeedToInstallSomething))
            {
                installers.Add(installerType);
            }
        }

        public IReadOnlyCollection<Type> Installers => installers;

        readonly HashSet<Type> installers = [];

        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);
    }

    public object GetDiagnostics() => settings.Installers.Select(i => i.FullName).ToArray();

    public async Task RunInstallers(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        foreach (var installerType in settings.Installers)
        {
            var installer = (INeedToInstallSomething)ActivatorUtilities.CreateInstance(serviceProvider, installerType);
            await installer.Install(settings.InstallationUserName, cancellationToken).ConfigureAwait(false);
        }
    }
}
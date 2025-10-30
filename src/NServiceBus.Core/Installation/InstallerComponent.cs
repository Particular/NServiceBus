#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Installation;
using Microsoft.Extensions.DependencyInjection;
using Settings;

class InstallerComponent(InstallerComponent.Settings settings)
{
    public void Initialize(IReadOnlySettings globalSettings) => globalSettings.AddStartupDiagnosticsSection("Installation", new
    {
        InstallersEnabled = settings.Installers.Select(i => i.Name).ToArray()
    });

    public async Task RunInstallers(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        foreach (var installer in settings.Installers)
        {
            await installer.Install(serviceProvider, settings.InstallationUserName, cancellationToken).ConfigureAwait(false);
        }
    }

    public class Settings
    {
        public string InstallationUserName { get; set; } = Environment.OSVersion.Platform == PlatformID.Win32NT ? $"{Environment.UserDomainName}\\{Environment.UserName}" : Environment.UserName;

        public void Add<TInstaller>() where TInstaller : class, INeedToInstallSomething => installers.Add(new Installer<TInstaller>());

        public void AddScannedInstallers(IEnumerable<Type> scannedTypes)
        {
            foreach (var installerType in scannedTypes.Where(IsINeedToInstallSomething))
            {
                var installerWrapperType = typeof(Installer<>).MakeGenericType(installerType);
                installers.Add(((IInstaller)Activator.CreateInstance(installerWrapperType)!)!);
            }
        }

        public IReadOnlyCollection<IInstaller> Installers => installers;

        readonly HashSet<IInstaller> installers = [];

        static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);

        class Installer<T> : IInstaller where T : class, INeedToInstallSomething
        {
            public async Task Install(IServiceProvider serviceProvider, string identity, CancellationToken cancellationToken = default)
            {
                // Deliberately not using the factory because installers are only resolved at startup once
                var installer = ActivatorUtilities.CreateInstance<T>(serviceProvider);

                try
                {
                    await installer.Install(identity, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    if (installer is IAsyncDisposable asyncDisposableInstaller)
                    {
                        await asyncDisposableInstaller.DisposeAsync().ConfigureAwait(false);
                    }
                    else if (installer is IDisposable disposableInstaller)
                    {
                        disposableInstaller.Dispose();
                    }
                }
            }

            public string Name { get; } = typeof(T).FullName!;

            public bool Equals(IInstaller? other) => other?.Name == Name;

            public override int GetHashCode() => Name.GetHashCode();
        }


        public interface IInstaller : IEquatable<IInstaller>
        {
            string Name { get; }

            Task Install(IServiceProvider serviceProvider, string identity, CancellationToken cancellationToken = default);
        }
    }
}
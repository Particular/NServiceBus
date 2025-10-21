#nullable enable
namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Installation;

class InstallerRegistry
{
    public void Add<TInstaller>() where TInstaller : class, INeedToInstallSomething => installers.Add(typeof(TInstaller));

    public IEnumerable<Type> GetInstallers() => installers;

    public void AddScannedInstallers(List<Type> scannedTypes)
    {
        foreach (var installerType in scannedTypes.Where(IsINeedToInstallSomething))
        {
            installers.Add(installerType);
        }
    }

    readonly HashSet<Type> installers = [];

    static bool IsINeedToInstallSomething(Type t) => typeof(INeedToInstallSomething).IsAssignableFrom(t);
}
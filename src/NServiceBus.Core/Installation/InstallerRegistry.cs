#nullable enable
namespace NServiceBus;

using System;
using System.Collections.Generic;
using Installation;

class InstallerRegistry
{
    public void Add<TInstaller>() where TInstaller : class, INeedToInstallSomething => installers.Add(typeof(TInstaller));

    public void Add(Type installerType)
    {
        //todo: validation
        installers.Add(installerType);
    }

    public IEnumerable<Type> GetInstallers() => installers;

    readonly HashSet<Type> installers = [];
}
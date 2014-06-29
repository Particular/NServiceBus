using System;
using System.Reflection;
using NServiceBus;

//binding redirect in code to avoid need to update the bindingredirect in app.config for TestAssembly.dll
public static class ModuleInitializer
{
    public static void Initialize()
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    }

    static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        if (args.Name.StartsWith("NServiceBus.Core,"))
        {
            return typeof(IMessage).Assembly;
        }
        return null;
    }
}
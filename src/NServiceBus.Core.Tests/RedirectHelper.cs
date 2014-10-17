using System;
using System.Reflection;
using NServiceBus;
using NUnit.Framework;

//binding redirect in code to avoid need to update the bindingredirect in app.config for TestAssembly.dll
[SetUpFixture]
public class RedirectHelper
{

    [SetUp]
    public void Initialize()
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
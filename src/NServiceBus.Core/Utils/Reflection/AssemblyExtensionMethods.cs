#nullable enable

namespace NServiceBus;

using System;
using System.Reflection;

static class AssemblyExtensionMethods
{
    public static bool IsParticularAssembly(this Assembly assembly)
    {
        var publicKeyToken = assembly.GetName().GetPublicKeyToken();
        return publicKeyToken.SequenceEqual(nsbPublicKeyToken);
    }

    static readonly byte[] nsbPublicKeyToken = typeof(AssemblyExtensionMethods).Assembly.GetName().GetPublicKeyToken()!;
}
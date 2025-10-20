#nullable enable
namespace NServiceBus.Core.SourceGen;

using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

public record struct CompilationAssemblyDetails(string SimpleName, string Identity)
{
    public static CompilationAssemblyDetails FromAssembly(IAssemblySymbol assembly) => new(assembly.Name, assembly.Identity.GetDisplayName());

    const string NamePrefix = "GeneratedTypeRegistrations_";
    const int HashBytesToUse = 10;

    public readonly string ToGenerationClassName()
    {
        var sb = new StringBuilder(NamePrefix, NamePrefix.Length + SimpleName.Length + 1 /* for _ separator*/ + (HashBytesToUse * 2))
            .Append(SimpleName.Replace('.', '_'))
            .Append('_');

        using var sha = SHA256.Create();

        var identityBytes = Encoding.UTF8.GetBytes(Identity);
        var hashBytes = sha.ComputeHash(identityBytes);
        for (var i = 0; i < HashBytesToUse; i++)
        {
            _ = sb.Append(hashBytes[i].ToString("x2"));
        }

        return sb.ToString();
    }
}
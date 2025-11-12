#nullable enable
namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

static class AssemblyValidator
{
    public static void ValidateAssemblyFile(string assemblyPath, out bool shouldLoad, out string reason)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var file = new PEReader(stream);
        var hasMetadata = false;

        try
        {
            hasMetadata = file.HasMetadata;
        }
        catch (BadImageFormatException) { }

        if (!hasMetadata)
        {
            shouldLoad = false;
            reason = "File is not a .NET assembly.";
            return;
        }

        var reader = file.GetMetadataReader();
        var assemblyDefinition = reader.GetAssemblyDefinition();

        if (!assemblyDefinition.PublicKey.IsNil)
        {
            var publicKey = reader.GetBlobBytes(assemblyDefinition.PublicKey);
            var publicKeyToken = GetPublicKeyToken(publicKey);

            var inspect = InspectAssembly(publicKeyToken);

            if (!inspect.ShouldLoad)
            {
                shouldLoad = false;
                reason = inspect.Reason;
                return;
            }
        }

        shouldLoad = true;
        reason = "File is a .NET assembly.";
    }

    public static bool IsAssemblyToSkip(AssemblyName assemblyName) => InspectAssembly(assemblyName) is { ShouldLoad: false };

    static InspectedAssembly InspectAssembly(AssemblyName assemblyName)
    {
        var tokenString = Convert.ToHexString(assemblyName.GetPublicKeyToken() ?? []);
        return InspectAssembly(tokenString);
    }

    static string GetPublicKeyToken(byte[] publicKey)
    {
        Span<byte> publicKeyToken = stackalloc byte[20];
        _ = SHA1.HashData(publicKey, publicKeyToken);
        var lastEightBytes = publicKeyToken.Slice(publicKeyToken.Length - 8, 8);
        lastEightBytes.Reverse();
        return Convert.ToHexString(lastEightBytes);
    }

    static InspectedAssembly InspectAssembly(string tokenString) =>
        tokenString switch
        {
            // Microsoft tokens
            "B77A5C561934E089" or "7CEC85D7BEA7798E" or "B03F5F7F11D50A3A" or "31BF3856AD364E35" or "CC7B13FFCD2DDD51" or "ADB9793829DDAE60" or "7E34167DCC6D6D8C" or "23EC7FC2D6EAA4A5" => new InspectedAssembly(false, "File is a .NET runtime assembly."),
            // Particular tokens
            "9FC386479F8A226C" => new InspectedAssembly(false, "File is a Particular assembly."),
            _ => new InspectedAssembly(true, null),
        };

    readonly struct InspectedAssembly(bool shouldLoad, string? reason)
    {
        [MemberNotNullWhen(false, nameof(Reason))]
        public bool ShouldLoad { get; } = shouldLoad;

        public string? Reason { get; } = reason;
    }
}
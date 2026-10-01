#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Reflection;
using Microsoft.CodeAnalysis;

static class SemanticModelExtensions
{
    // The IModuleSymbol API is newer than the Roslyn version we compile against, so it is bound reflectively.
    static readonly Func<IModuleSymbol, int>? MemorySafetyRulesVersionAccessor = CreateMemorySafetyRulesVersionAccessor();

    extension(SemanticModel semanticModel)
    {
        // Under the updated memory safety rules every extern member must be marked either safe or unsafe.
        public bool UsesUpdatedMemorySafetyRules()
        {
            const int UpdatedMemorySafetyRulesVersion = 2;

            return MemorySafetyRulesVersionAccessor is { } getVersion
                ? getVersion(semanticModel.Compilation.SourceModule) >= UpdatedMemorySafetyRulesVersion
                : semanticModel.SyntaxTree.Options.Features.ContainsKey("updated-memory-safety-rules");
        }
    }

    static Func<IModuleSymbol, int>? CreateMemorySafetyRulesVersionAccessor()
    {
        var getter = typeof(IModuleSymbol).GetProperty("MemorySafetyRulesVersion")?.GetMethod;
        var returnType = getter?.ReturnType;
        // Delegate binding accepts an enum-valued getter through its underlying int type; anything else would throw.
        var isIntCompatible = returnType is not null && (returnType == typeof(int) || (returnType.IsEnum && Enum.GetUnderlyingType(returnType) == typeof(int)));
        return isIntCompatible ? (Func<IModuleSymbol, int>)getter!.CreateDelegate(typeof(Func<IModuleSymbol, int>)) : null;
    }
}

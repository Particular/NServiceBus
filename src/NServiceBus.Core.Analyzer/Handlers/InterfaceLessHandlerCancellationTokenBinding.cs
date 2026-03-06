namespace NServiceBus.Core.Analyzer.Handlers;

using System.Linq;
using Microsoft.CodeAnalysis;

static class InterfaceLessHandlerCancellationTokenBinding
{
    public static bool IsInterfaceLessHandlerWithBoundCancellationToken(IMethodSymbol method, Compilation compilation)
    {
        var handlerAttribute = compilation.GetTypeByMetadataName("NServiceBus.HandlerAttribute");
        var messageHandlerContext = compilation.GetTypeByMetadataName("NServiceBus.IMessageHandlerContext");
        var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

        if (method.MethodKind != MethodKind.Ordinary ||
            method.Name != "Handle" ||
            method.Parameters.Length < 3 ||
            method.ContainingType is null ||
            handlerAttribute is null ||
            messageHandlerContext is null ||
            cancellationTokenType is null ||
            !method.ContainingType.HasAttribute(handlerAttribute))
        {
            return false;
        }

        var secondParam = method.Parameters[1];
        if (!secondParam.Type.Equals(messageHandlerContext, SymbolEqualityComparer.IncludeNullability))
        {
            return false;
        }

        return method.Parameters.Skip(2)
            .Any(param => param.Type.Equals(cancellationTokenType, SymbolEqualityComparer.IncludeNullability));
    }
}
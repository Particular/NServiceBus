namespace NServiceBus.Core.Analyzer.Utility;

using System.Text;

static class InterceptorMethodNameBuilder
{
    public static string Build(string prefix, string typeName, string fullyQualifiedTypeName)
    {
        var sb = new StringBuilder(prefix.Length + typeName.Length + 1 + 16)
            .Append(prefix)
            .Append(typeName)
            .Append('_');

        var hash = NonCryptographicHash.GetHash(fullyQualifiedTypeName);
        sb.Append(hash.ToString("x16"));

        return sb.ToString();
    }
}
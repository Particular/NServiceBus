namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    class SemanticModelCache
    {
        Compilation compilation;
#pragma warning disable PS0025 // Dictionary keys should implement IEquatable<T> - A SemanticModelCache is not long-lived and is meant to use reference equality
        Dictionary<SyntaxTree, SemanticModel> dict;
#pragma warning restore PS0025 // Dictionary keys should implement IEquatable<T>

        public SemanticModelCache(Compilation compilation, SyntaxTree originalSyntaxTree, SemanticModel originalSemanticModel)
        {
            this.compilation = compilation;
            dict = new Dictionary<SyntaxTree, SemanticModel>
            {
                [originalSyntaxTree] = originalSemanticModel
            };
        }

        public SemanticModel GetFor(SyntaxNode node)
        {
            return GetFor(node.SyntaxTree);
        }

        public SemanticModel GetFor(SyntaxTree tree)
        {
            if (dict.TryGetValue(tree, out var model))
            {
                return model;
            }

            model = compilation.GetSemanticModel(tree);

            dict[tree] = model;

            return model;
        }
    }
}

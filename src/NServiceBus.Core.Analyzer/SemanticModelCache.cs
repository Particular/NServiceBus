namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    class SemanticModelCache
    {
        Compilation compilation;
        Dictionary<SyntaxTree, SemanticModel> dict;

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

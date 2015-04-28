namespace MiniSharpCompiler
{
    using Microsoft.CodeAnalysis;

    internal static class Extensions
    {
        public static SyntaxNode DeclaringSyntaxNode(this SyntaxNode node, SemanticModel model)
        {
            return model.GetSymbolInfo(node).Symbol?.DeclaringSyntaxReferences[0].GetSyntax();
        }
    }
}
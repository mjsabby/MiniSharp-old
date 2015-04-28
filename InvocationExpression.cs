namespace MiniSharpCompiler
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.Visit(node);
        }
    }
}
namespace MiniSharpCompiler
{
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	public partial class LLVMIRGenerationVisitor
	{
		public override void VisitCastExpression(CastExpressionSyntax node)
		{
			base.VisitCastExpression(node);
		}
	}
}
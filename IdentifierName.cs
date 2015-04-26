namespace MiniSharpCompiler
{
	using LLVMSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	public partial class LLVMIRGenerationVisitor
	{
		/// <summary>
        /// Variables, fields, method names
        /// </summary>
	    public override void VisitIdentifierName(IdentifierNameSyntax node)
		{
			var currentSymbolTable = this.symbolTable.Peek().Locals;
			var symbol = currentSymbolTable[node.Identifier.Text].Item2;

			if (node.Parent is AssignmentExpressionSyntax)
			{
				this.valueStack.Push(symbol);
			}
			else
			{
				this.Push(node, LLVM.BuildLoad(this.builder, symbol, string.Empty));
			}
	    }
	}
}
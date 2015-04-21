namespace MiniSharpCompiler
{
	using LLVMSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	public partial class LLVMIRGenerationVisitor
	{
		/// <summary>
		/// 8.9.1
		/// </summary>
		public override void VisitBreakStatement(BreakStatementSyntax node)
		{
			var controlFlowTarget = this.controlFlowStack.Peek();
			LLVM.BuildBr(this.builder, controlFlowTarget.End);
			LLVM.PositionBuilderAtEnd(this.builder, LLVM.AppendBasicBlock(this.function, "UnreachableBreak"));
		}
	}
}
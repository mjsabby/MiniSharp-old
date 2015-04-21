namespace MiniSharpCompiler
{
	using LLVMSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	public partial class LLVMIRGenerationVisitor
	{
		/// <summary>
		/// 8.9.2
		/// </summary>
		public override void VisitContinueStatement(ContinueStatementSyntax node)
		{
			var controlFlowTarget = this.controlFlowStack.Peek();
			LLVM.BuildBr(this.builder, controlFlowTarget.Condition);
			LLVM.PositionBuilderAtEnd(this.builder, LLVM.AppendBasicBlock(this.function, "UnreachableContinue"));
		}
	}
}
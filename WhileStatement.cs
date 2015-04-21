namespace MiniSharpCompiler
{
	using LLVMSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	public partial class LLVMIRGenerationVisitor
	{
		/// <summary>
		/// 8.8.1
		/// </summary>
		public override void VisitWhileStatement(WhileStatementSyntax node)
		{
			LLVMBasicBlockRef condBB = LLVM.AppendBasicBlock(this.function, "while.cond");
			LLVMBasicBlockRef bodyBB = LLVM.AppendBasicBlock(this.function, "while.body");
			LLVMBasicBlockRef endBB = LLVM.AppendBasicBlock(this.function, "while.end");

			this.controlFlowStack.Push(new ControlFlowTarget(condBB, endBB));

			LLVM.BuildBr(this.builder, condBB);

			// condition
			LLVM.PositionBuilderAtEnd(this.builder, condBB);
			LLVM.BuildCondBr(this.builder, this.Pop(node.Condition), bodyBB, endBB);

			// body
			LLVM.PositionBuilderAtEnd(this.builder, bodyBB);
			this.EnterScope();
			this.Visit(node.Statement);
			this.LeaveScope();
			LLVM.BuildBr(this.builder, condBB);

			// end
			LLVM.PositionBuilderAtEnd(this.builder, endBB);

			this.controlFlowStack.Pop();
		}
	}
}
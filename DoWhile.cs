namespace MiniSharpCompiler
{
	using LLVMSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	public partial class LLVMIRGenerationVisitor
	{
		/// <summary>
		/// 8.8.2
		/// </summary>
		public override void VisitDoStatement(DoStatementSyntax node)
		{
			LLVMBasicBlockRef condBB = LLVM.AppendBasicBlock(this.function, "do.cond");
			LLVMBasicBlockRef bodyBB = LLVM.AppendBasicBlock(this.function, "do.body");
			LLVMBasicBlockRef endBB = LLVM.AppendBasicBlock(this.function, "do.end");

			this.controlFlowStack.Push(new ControlFlowTarget(condBB, endBB));

			LLVM.BuildBr(this.builder, bodyBB);

			// body
			LLVM.PositionBuilderAtEnd(this.builder, bodyBB);
			this.EnterScope();
			this.Visit(node.Statement);
			this.LeaveScope();
			LLVM.BuildBr(this.builder, condBB);

			// condition
			LLVM.PositionBuilderAtEnd(this.builder, condBB);
			LLVM.BuildCondBr(this.builder, this.Pop(node.Condition), bodyBB, endBB);

			LLVM.PositionBuilderAtEnd(this.builder, endBB);
		}
	}
}
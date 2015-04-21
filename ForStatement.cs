namespace MiniSharpCompiler
{
	using LLVMSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	public partial class LLVMIRGenerationVisitor
	{
		/// <summary>
		/// For statement
		/// </summary>
		public override void VisitForStatement(ForStatementSyntax node)
		{
			LLVMBasicBlockRef condBB = LLVM.AppendBasicBlock(this.function, "for.cond");
			LLVMBasicBlockRef bodyBB = LLVM.AppendBasicBlock(this.function, "for.body");
			LLVMBasicBlockRef incBB = LLVM.AppendBasicBlock(this.function, "for.inc");
			LLVMBasicBlockRef endBB = LLVM.AppendBasicBlock(this.function, "for.end");

			this.controlFlowStack.Push(new ControlFlowTarget(condBB, endBB));
			this.EnterScope();

			if (node.Declaration != null)
			{
				this.Visit(node.Declaration);
			}

			foreach (var initializer in node.Initializers)
			{
				this.Visit(initializer);
			}

			LLVM.BuildBr(this.builder, condBB);

			// condition
			LLVM.PositionBuilderAtEnd(this.builder, condBB);
			LLVM.BuildCondBr(this.builder, this.Pop(node.Condition), bodyBB, endBB);

			// body
			LLVM.PositionBuilderAtEnd(this.builder, bodyBB);
			this.Visit(node.Statement);
			LLVM.BuildBr(this.builder, incBB);

			// inc
			LLVM.PositionBuilderAtEnd(this.builder, incBB);
			foreach (var incrementor in node.Incrementors)
			{
				this.Visit(incrementor);
			}
			LLVM.BuildBr(this.builder, condBB);

			// end
			LLVM.PositionBuilderAtEnd(this.builder, endBB);

			this.controlFlowStack.Pop();
		}
	}
}
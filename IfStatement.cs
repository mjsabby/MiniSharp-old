namespace MiniSharpCompiler
{
	using LLVMSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	public partial class LLVMIRGenerationVisitor
	{
		/// <summary>
		/// 8.7.1
		/// </summary>
		public override void VisitIfStatement(IfStatementSyntax node)
		{
			LLVMBasicBlockRef ifThen = LLVM.AppendBasicBlock(this.function, "if.then");
			LLVMBasicBlockRef ifElse = LLVM.AppendBasicBlock(this.function, "if.else");
			LLVMBasicBlockRef end = LLVM.AppendBasicBlock(this.function, "if.end");

			LLVMBasicBlockRef targtBB = node.Else != null ? ifElse : end;
			LLVM.BuildCondBr(this.builder, this.Pop(node.Condition), ifThen, targtBB);

			// true case
			LLVM.PositionBuilderAtEnd(this.builder, ifThen);
			this.EnterScope();
			this.Visit(node.Statement);
			this.LeaveScope();
			LLVM.BuildBr(this.builder, end);

			// else case
			if (node.Else != null)
			{
				LLVM.PositionBuilderAtEnd(this.builder, ifElse);
				this.EnterScope();
				this.Visit(node.Else.Statement);
				this.LeaveScope();
				LLVM.BuildBr(this.builder, end);
			}

			// end
			LLVM.PositionBuilderAtEnd(this.builder, end);
		}
	}
}
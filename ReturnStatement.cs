namespace MiniSharpCompiler
{
	using LLVMSharp;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	public partial class LLVMIRGenerationVisitor
	{
		/// <summary>
		/// 8.9.4
		/// </summary>
		public override void VisitReturnStatement(ReturnStatementSyntax node)
		{
			var returnType = this.semanticModel.GetTypeInfo(node.Expression);
			if (returnType.Type.SpecialType == SpecialType.System_Void)
			{
				LLVM.BuildRetVoid(this.builder);
			}
			else
			{
				LLVM.BuildRet(this.builder, this.Pop(node.Expression));
			}

			LLVM.PositionBuilderAtEnd(this.builder, LLVM.AppendBasicBlock(this.function, "UnreachableReturn"));
		}
	}
}
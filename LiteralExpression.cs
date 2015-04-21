namespace MiniSharpCompiler
{
	using System;
	using LLVMSharp;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	public partial class LLVMIRGenerationVisitor
	{
		/// <summary>
		/// Section 2.4.4 Literals
		/// </summary>
		public override void VisitLiteralExpression(LiteralExpressionSyntax node)
		{
			SyntaxKind kind = node.Kind();
			TypeInfo typeInfo = this.semanticModel.GetTypeInfo(node);
			LLVMTypeRef llvmTypeRef = typeInfo.LLVMTypeRef();
			LLVMValueRef operand;

			switch (kind)
			{
				case SyntaxKind.TrueLiteralExpression:
					operand = LLVM.ConstInt(TypeSystem.Int1Type, N: 1, SignExtend: False);
					break;
				case SyntaxKind.FalseLiteralExpression:
					operand = LLVM.ConstInt(TypeSystem.Int1Type, N: 0, SignExtend: False);
					break;
				case SyntaxKind.CharacterLiteralExpression:
					operand = LLVM.ConstInt(TypeSystem.Int16Type, (char)node.Token.Value, SignExtend: False);
					break;
				case SyntaxKind.NullLiteralExpression:
					operand = LLVM.ConstNull(TypeSystem.NullType);
					break;
				case SyntaxKind.NumericLiteralExpression:
					switch (typeInfo.Type.SpecialType)
					{
						case SpecialType.System_Int32: // var x = 1;
							operand = LLVM.ConstInt(llvmTypeRef, (ulong)((int)node.Token.Value), SignExtend: True);
							break;
						case SpecialType.System_UInt32: // var x = 1U;
							operand = LLVM.ConstInt(llvmTypeRef, (uint) node.Token.Value, SignExtend: False);
							break;
						case SpecialType.System_Int64: // var x = 1L;
							operand = LLVM.ConstInt(llvmTypeRef, (ulong)((long)node.Token.Value), SignExtend: True);
							break;
						case SpecialType.System_UInt64: // var x = 1UL;
							operand = LLVM.ConstInt(llvmTypeRef, (ulong)node.Token.Value, SignExtend: False);
							break;
						case SpecialType.System_Single: // var x = 1F;
							operand = LLVM.ConstReal(llvmTypeRef, (float)node.Token.Value);
							break;
						case SpecialType.System_Double: // var x = 1D;
							operand = LLVM.ConstReal(llvmTypeRef, (double)node.Token.Value);
							break;
						default:
							throw new Exception("Unreachable");
					}
					break;
				case SyntaxKind.StringLiteralExpression:
					throw new NotImplementedException("Strings are not yet supported");
				default:
					throw new Exception("Unreachable");
			}

			this.Push(node, operand);
		}
	}
}
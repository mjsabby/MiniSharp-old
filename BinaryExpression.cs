namespace MiniSharpCompiler
{
	using System;
	using LLVMSharp;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;

	public partial class LLVMIRGenerationVisitor
	{
		public override void VisitBinaryExpression(BinaryExpressionSyntax node)
		{
			SyntaxKind kind = node.Kind();
			switch (kind)
			{
				case SyntaxKind.AddExpression:
					this.AddExpression(node, node.Left, node.Right);
					break;
				case SyntaxKind.SubtractExpression:
					this.SubExpression(node, node.Left, node.Right);
					break;
				case SyntaxKind.MultiplyExpression:
					this.MulExpression(node, node.Left, node.Right);
					break;
				case SyntaxKind.DivideExpression:
					this.DivExpression(node, node.Left, node.Right);
					break;
				case SyntaxKind.ModuloExpression:
					this.ModExpression(node, node.Left, node.Right);
					break;
				case SyntaxKind.LeftShiftExpression:
					this.ShlExpression(node, node.Left, node.Right);
					break;
				case SyntaxKind.RightShiftExpression:
					this.ShrExpression(node, node.Left, node.Right);
					break;
				case SyntaxKind.BitwiseAndExpression:
					this.BitAndExpression(node, node.Left, node.Right);
					break;
				case SyntaxKind.BitwiseOrExpression:
					this.BitOrExpression(node, node.Left, node.Right);
					break;
				case SyntaxKind.ExclusiveOrExpression:
					this.XorExpression(node, node.Left, node.Right);
					break;
				case SyntaxKind.LogicalAndExpression:
					this.ConditionalAndExpression(node);
					break;
				case SyntaxKind.LogicalOrExpression:
					this.ConditionalOrExpression(node);
					break;
				case SyntaxKind.EqualsExpression:
				case SyntaxKind.NotEqualsExpression:
				case SyntaxKind.LessThanExpression:
				case SyntaxKind.LessThanOrEqualExpression:
				case SyntaxKind.GreaterThanExpression:
				case SyntaxKind.GreaterThanOrEqualExpression:
					this.RelEqExpression(node);
					break;
				default:
					throw new Exception("Unreachable");
			}
		}

		private void AddExpression(ExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right, bool visit = true)
		{
			var nodeType = this.semanticModel.GetTypeInfo(node).Type;
			switch (nodeType.SpecialType)
			{
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMAdd, "add", visit);
					break;
				case SpecialType.System_Single:
				case SpecialType.System_Double:
				case SpecialType.System_Decimal:
					this.BinOp(node, left, right, LLVMOpcode.LLVMFAdd, "fadd", visit);
					break;
				case SpecialType.System_String:
					throw new NotImplementedException("String concatenation is not implemented");
			}
		}

		private void SubExpression(ExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right, bool visit = true)
		{
			var nodeType = this.semanticModel.GetTypeInfo(node).Type;
			switch (nodeType.SpecialType)
			{
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMSub, "sub", visit);
					break;
				case SpecialType.System_Single:
				case SpecialType.System_Double:
				case SpecialType.System_Decimal:
					this.BinOp(node, left, right, LLVMOpcode.LLVMFSub, "fsub", visit);
					break;
				default:
					throw new NotImplementedException("Operator overloading not yet implemented");
			}
		}

		private void MulExpression(ExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right, bool visit = true)
		{
			var nodeType = this.semanticModel.GetTypeInfo(node).Type;
			switch (nodeType.SpecialType)
			{
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMMul, "mul", visit);
					break;
				case SpecialType.System_Single:
				case SpecialType.System_Double:
				case SpecialType.System_Decimal:
					this.BinOp(node, left, right, LLVMOpcode.LLVMFMul, "fmul", visit);
					break;
				default:
					throw new NotImplementedException("Operator overloading not yet implemented");
			}
		}

		private void DivExpression(ExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right, bool visit = true)
		{
			var nodeType = this.semanticModel.GetTypeInfo(node).Type;
			switch (nodeType.SpecialType)
			{
				case SpecialType.System_Int32:
				case SpecialType.System_Int64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMSDiv, "sdiv", visit);
					break;
				case SpecialType.System_UInt32:
				case SpecialType.System_UInt64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMUDiv, "udiv", visit);
					break;
				case SpecialType.System_Single:
				case SpecialType.System_Double:
				case SpecialType.System_Decimal:
					this.BinOp(node, left, right, LLVMOpcode.LLVMFDiv, "fdiv", visit);
					break;
				default:
					throw new NotImplementedException("Operator overloading not yet implemented");
			}
		}

		private void ModExpression(ExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right, bool visit = true)
		{
			var nodeType = this.semanticModel.GetTypeInfo(node).Type;
			switch (nodeType.SpecialType)
			{
				case SpecialType.System_Int32:
				case SpecialType.System_Int64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMSRem, "srem", visit);
					break;
				case SpecialType.System_UInt32:
				case SpecialType.System_UInt64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMURem, "urem", visit);
					break;
				case SpecialType.System_Single:
				case SpecialType.System_Double:
				case SpecialType.System_Decimal:
					this.BinOp(node, left, right, LLVMOpcode.LLVMFRem, "frem", visit);
					break;
				default:
					throw new NotImplementedException("Operator overloading not yet implemented");
			}
		}

		private void ShlExpression(ExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right, bool visit = true)
		{
			var nodeType = this.semanticModel.GetTypeInfo(node).Type;
			switch (nodeType.SpecialType)
			{
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMShl, "shl", visit);
					break;
				default:
					throw new NotImplementedException("Operator overloading not yet implemented");
			}
		}

		private void ShrExpression(ExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right, bool visit = true)
		{
			var leftNodeType = this.semanticModel.GetTypeInfo(left).Type;
			switch (leftNodeType.SpecialType)
			{
				case SpecialType.System_Int32:
				case SpecialType.System_Int64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMAShr, "ashr", visit);
					break;
				case SpecialType.System_UInt32:
				case SpecialType.System_UInt64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMLShr, "lshr", visit);
					break;
				default:
					throw new NotImplementedException("Operator overloading not yet implemented");
			}
		}

		private void BitAndExpression(ExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right, bool visit = true)
		{
			var nodeType = this.semanticModel.GetTypeInfo(node).Type;
			switch (nodeType.SpecialType)
			{
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMAnd, "and", visit);
					break;
				default:
					throw new NotImplementedException("Operator overloading not yet implemented");
			}
		}

		private void BitOrExpression(ExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right, bool visit = true)
		{
			var nodeType = this.semanticModel.GetTypeInfo(node).Type;
			switch (nodeType.SpecialType)
			{
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMOr, "or", visit);
					break;
				default:
					throw new NotImplementedException("Operator overloading not yet implemented");
			}
		}

		private void XorExpression(ExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right, bool visit = true)
		{
			var nodeType = this.semanticModel.GetTypeInfo(node).Type;
			switch (nodeType.SpecialType)
			{
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
					this.BinOp(node, left, right, LLVMOpcode.LLVMXor, "xor", visit);
					break;
				default:
					throw new NotImplementedException("Operator overloading not yet implemented");
			}
		}

		private void ConditionalAndExpression(BinaryExpressionSyntax node)
		{
			LLVMBasicBlockRef incoming = LLVM.GetInsertBlock(this.builder);
			LLVMBasicBlockRef lorRhs = LLVM.AppendBasicBlock(this.function, "lor.rhs");
			LLVMBasicBlockRef lorEnd = LLVM.AppendBasicBlock(this.function, "lor.end");

			var lhs = this.Pop(node.Left);

			LLVM.BuildCondBr(this.builder, lhs, lorEnd, lorRhs);

			LLVM.PositionBuilderAtEnd(this.builder, lorRhs);
			var rhs = this.Pop(node.Right);
			LLVM.BuildBr(this.builder, lorEnd);

			LLVM.PositionBuilderAtEnd(this.builder, lorEnd);
			var phi = LLVM.BuildPhi(this.builder, LLVM.Int1Type(), "phi");
			var falseValue = LLVM.ConstInt(LLVM.Int1Type(), 1, False);

			LLVM.AddIncoming(phi, out falseValue, out incoming, 1);
			LLVM.AddIncoming(phi, out rhs, out lorRhs, 1);

			LLVM.PositionBuilderAtEnd(this.builder, lorEnd);

			this.Push(node, phi);
		}

		private void ConditionalOrExpression(BinaryExpressionSyntax node)
		{
			LLVMBasicBlockRef incoming = LLVM.GetInsertBlock(this.builder);
			LLVMBasicBlockRef lorRhs = LLVM.AppendBasicBlock(this.function, "lor.rhs");
			LLVMBasicBlockRef lorEnd = LLVM.AppendBasicBlock(this.function, "lor.end");

			var lhs = this.Pop(node.Left);

			LLVM.BuildCondBr(this.builder, lhs, lorEnd, lorRhs);

			LLVM.PositionBuilderAtEnd(this.builder, lorRhs);
			var rhs = this.Pop(node.Right);
			LLVM.BuildBr(this.builder, lorEnd);

			LLVM.PositionBuilderAtEnd(this.builder, lorEnd);
			var phi = LLVM.BuildPhi(this.builder, LLVM.Int1Type(), "phi");
			var trueValue = LLVM.ConstInt(LLVM.Int1Type(), 1, False);

			LLVM.AddIncoming(phi, out trueValue, out incoming, 1);
			LLVM.AddIncoming(phi, out rhs, out lorRhs, 1);

			LLVM.PositionBuilderAtEnd(this.builder, lorEnd);

			this.Push(node, phi);
		}

		private void RelEqExpression(BinaryExpressionSyntax node)
		{
			var left = this.semanticModel.GetTypeInfo(node.Left);
			var right = this.semanticModel.GetTypeInfo(node.Right);

			var leftType = left.Type;
			var rightType = right.Type;

			if (!leftType.Equals(rightType))
			{
				throw new Exception("Type mismatch exception");
			}

			if (leftType.SpecialType == SpecialType.System_Double || leftType.SpecialType == SpecialType.System_Single)
			{
				throw new Exception("Single/Double relational expression");
			}

			this.valueStack.Push(LLVM.BuildICmp(this.builder, TypeSystem.IntPredicate(node.Kind()), this.Pop(node.Left), this.Pop(node.Right), "cmptmp"));
		}

		private void BinOp(ExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right, LLVMOpcode opcode, string name, bool visit = true)
		{
			var lhs = visit ? this.Pop(left) : this.OnlyPop(left);
			var rhs = visit ? this.Pop(right) : this.OnlyPop(right);
            this.Push(node, LLVM.BuildBinOp(builder, opcode, lhs , rhs, name));
		}
	}
}
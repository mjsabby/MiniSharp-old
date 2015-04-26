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
        /// 7.6.9
        /// </summary>
        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            LLVMValueRef operand;
            LLVMOpcode opcode;
            LLVMValueRef opValue;

            TypeInfo typeInfo = this.semanticModel.GetTypeInfo(node.Operand);
            switch (typeInfo.ConvertedType.SpecialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Char:
                    opcode = node.Kind() == SyntaxKind.PostIncrementExpression ? LLVMOpcode.LLVMAdd : LLVMOpcode.LLVMSub;
                    opValue = LLVM.ConstInt(typeInfo.LLVMTypeRef(), 1, typeInfo.IsSignExtended());
                    break;
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                    opcode = node.Kind() == SyntaxKind.PostIncrementExpression ? LLVMOpcode.LLVMFAdd : LLVMOpcode.LLVMFSub;
                    opValue = LLVM.ConstReal(typeInfo.LLVMTypeRef(), 1);
                    break;
                default:
                    throw new NotImplementedException("PostIncrementExpression/PostDecrementExpression overloading is not yet implemented");
            }

            switch (node.Operand.Kind())
            {
                case SyntaxKind.IdentifierName:
                    var currentSymbolTable = this.symbolTable.Peek().Locals;
                    var identifier = (IdentifierNameSyntax) node.Operand;
                    operand = currentSymbolTable[identifier.Identifier.Text].Item2;
                    break;
                case SyntaxKind.SimpleMemberAccessExpression:
                    throw new NotImplementedException("PostIncrementExpression/PostDecrementExpression for SimpleMemberAccessExpression is not implemented");
                case SyntaxKind.ElementAccessExpression:
                    throw new NotImplementedException("PostIncrementExpression/PostDecrementExpression for ElementAccessExpression is not implemented");
                default:
                    throw new Exception("Unreachable");
            }

            this.Visit(node.Operand);
            var inc = LLVM.BuildBinOp(this.builder, opcode, this.valueStack.Peek(), opValue, "postop");
            LLVM.BuildStore(this.builder, inc, operand);
        }
    }
}